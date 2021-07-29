using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Encryption.Cryptography;
using Microsoft.Data.Encryption.FileEncryption;

using Avro;
using Avro.Generic;
using Avro.File;
using Avro.Util;
using ColumnEncrypt.Data;
using ColumnEncrypt.Metadata;
using ColumnEncrypt.Util;

namespace ColumnEncrypt.DataProviders
{
    public class AvroDataReader : IColumnarDataReader, IDisposable
    {
        private Stream _stream;
        private Schema _schema;
        private string[] _fieldNames;
        private IList<FileEncryptionSettings> _fileEncryptionSettings;

        private IDictionary<string, EncryptionKeyStoreProvider> _encryptionKeyStoreProviders;

        public IList<FileEncryptionSettings> FileEncryptionSettings
        {
            get
            {
                if (_fileEncryptionSettings == null)
                {
                    LoadFileMetadata();
                    return _fileEncryptionSettings;
                }
                else
                {
                    return _fileEncryptionSettings;
                }
            }
        }

        public string[] FieldNames
        {
            get
            {
                if (_fieldNames == null)
                {
                    LoadFileMetadata();
                    return _fieldNames;
                }
                else
                {
                    return _fieldNames;
                }
            }
        }

        public LogicalTypeFactory logicalTypeFactory = LogicalTypeFactory.Instance;

        public AvroDataReader(Stream stream, IDictionary<string, EncryptionKeyStoreProvider> encryptionKeyStoreProviders)
        {
            _stream = stream;
            // _schema = Schema.Parse(schema);
            _encryptionKeyStoreProviders = encryptionKeyStoreProviders;
            logicalTypeFactory.Register(new EncryptedLogicalType());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IEnumerable<IColumn>> Read()
        {
            var columns = new List<ColumnData>();

            // TODO: Previous reads update the stream position so subsequent usses of the same stream need to re-set to zero. There's probabaly a better way to do this.
            _stream.Position = 0;

            using (var reader = DataFileReader<GenericRecord>.OpenReader(_stream, false))
            {
                // TODO: This is redundant to the LoadFileMetaData() code. Refactor.
                RecordSchema schema = (RecordSchema)reader.GetSchema();
                List<Field> fields = schema.Fields;

                foreach (var field in fields)
                {
                    Type dataType = GetFieldDataType(field.Schema);
                    var newColumn = new ColumnData(field.Name, dataType);
                    newColumn.Index = field.Pos;
                    columns.Add(newColumn);
                    // columns.Add(new ColumnData(field.Name, dataType));
                }

                // Read in record values

                while (reader.HasNext())
                {
                    GenericRecord record = reader.Next();

                    foreach (var column in columns)
                    {
                        object fieldData = null;
                        record.TryGetValue(column.Name, out fieldData);
                        column.AddColumnRecord(GetTypedValue(column.DataType, fieldData));
                    }
                }

            }

            var columnDataEnum = columns as IEnumerable<IColumn>;
            var result = new List<IEnumerable<IColumn>>();
            result.Add(columnDataEnum);
            return result;

        }

        public string[] GetFieldNames(RecordSchema schema, IFileReader<GenericRecord> reader)
        {
            List<string> fieldNames = new List<string>();

            foreach (var field in schema.Fields)
            {
                fieldNames.Add(field.Name);
            }

            return fieldNames.ToArray();
        }

        public void Dispose()
        {
            // TODO: Determine right way to dispose the Avro reader
            // throw new NotImplementedException();
        }

        private void LoadFileMetadata()
        {
            _fileEncryptionSettings = new List<FileEncryptionSettings>();

            using (var reader = DataFileReader<GenericRecord>.OpenReader(_stream, true))
            {
                RecordSchema schema = (RecordSchema)reader.GetSchema();

                // Set field names
                _fieldNames = GetFieldNames(schema, reader);

                // Load encryption metadata from the Avro document
                List<ColumnKeyInfo> columnKeyInfo = JsonSerializer.Deserialize<List<ColumnKeyInfo>>(reader.GetMetaString("columnKeyInfo"));
                List<ColumnMasterKeyInfo> masterKeyInfo = JsonSerializer.Deserialize<List<ColumnMasterKeyInfo>>(reader.GetMetaString("columnMasterKeyInfo"));

                // Set a default key from config. This is required for columns that are not part of encryption
                ColumnKeyInfo defaultDekInfo = columnKeyInfo.FirstOrDefault();
                ColumnMasterKeyInfo defaultKekInfo = masterKeyInfo.First(x => x.Name == defaultDekInfo.ColumnMasterKeyName);
                byte[] defaultDekBytes = Converter.FromHexString(defaultDekInfo.EncryptedColumnKey);

                KeyEncryptionKey defaultKek = new KeyEncryptionKey(defaultKekInfo.Name, defaultKekInfo.KeyPath, _encryptionKeyStoreProviders["AZURE_KEY_VAULT"]);

                foreach (var field in schema.Fields)
                {
                    string dekName = field.Schema.GetProperty("columnKeyName");
                    KeyEncryptionKey kek = null;
                    byte[] dekBytes = null;
                    EncryptionType encryptionType;
                    Schema.Type fieldDataType = field.Schema.Tag;

                    // If no encryption, set default values
                    if (String.IsNullOrEmpty(dekName))
                    {
                        encryptionType = EncryptionType.Plaintext;
                        dekName = "none";
                        dekBytes = defaultDekBytes;
                        kek = defaultKek;
                    }
                    else
                    {
                        dekName = dekName.Replace("\"", "").Replace("\\", "");
                        ColumnKeyInfo dekInfo = columnKeyInfo.First(x => x.Name == dekName);
                        dekBytes = Converter.FromHexString(dekInfo.EncryptedColumnKey);

                        ColumnMasterKeyInfo kekInfo = masterKeyInfo.First(x => x.Name == dekInfo.ColumnMasterKeyName);
                        kek = new KeyEncryptionKey(kekInfo.Name, kekInfo.KeyPath, _encryptionKeyStoreProviders["AZURE_KEY_VAULT"]);

                        string encryption = field.Schema.GetProperty("encryptionType").Replace("\"", "").Replace("\\", "");;

                        if (encryption.ToLower() == "randomized")
                            encryptionType = EncryptionType.Randomized;
                        else if (encryption.ToLower() == "deterministic")
                            encryptionType = EncryptionType.Deterministic;
                        else
                            encryptionType = EncryptionType.Plaintext;
                    }

                    ProtectedDataEncryptionKey protectedDek = new ProtectedDataEncryptionKey(dekName, kek, dekBytes);

                    FileEncryptionSettings columnEncryptionSettings = null;

                    if (fieldDataType == Schema.Type.Union)
                    {
                        var schemas = ((Avro.UnionSchema)field.Schema).Schemas;
                        fieldDataType = schemas[0].Tag;
                    }

                    switch (fieldDataType)
                    {
                        case Schema.Type.String:
                            columnEncryptionSettings = ColumnSettings.GetColumnEncryptionSettings<string>(protectedDek, encryptionType);
                            break;
                        case Schema.Type.Int:
                            columnEncryptionSettings = ColumnSettings.GetColumnEncryptionSettings<int>(protectedDek, encryptionType);
                            break;
                        case Schema.Type.Float:
                            columnEncryptionSettings = ColumnSettings.GetColumnEncryptionSettings<float>(protectedDek, encryptionType);
                            break;
                        case Schema.Type.Double:
                            columnEncryptionSettings = ColumnSettings.GetColumnEncryptionSettings<double>(protectedDek, encryptionType);
                            break;
                        case Schema.Type.Logical:
                            // columnEncryptionSettings = ColumnSettings.GetColumnEncryptionSettings<byte[]>(protectedDek, encryptionType);
                            columnEncryptionSettings = ColumnSettings.GetColumnEncryptionSettings<string>(protectedDek, encryptionType);
                            break;                    
                    }

                    // Type dataType = GetFieldDataType(fieldSchema);
                    // columnEncryptionSettings = ColumnSettings.GetColumnEncryptionSettings<>(protectedDek, encryptionType);
                    _fileEncryptionSettings.Add(columnEncryptionSettings);

                }

            }

        }

        /// <summary>
        /// Returns the C# type of an Avro field. For Union fields, the first Avro data type is used 
        /// </summary>
        /// <param name="fieldSchema"></param>
        /// <returns></returns>
        private Type GetFieldDataType(Schema fieldSchema)
        {
            Schema.Type fieldDataType = fieldSchema.Tag;

            if (fieldDataType == Schema.Type.Union)
            {
                var schemas = ((Avro.UnionSchema)fieldSchema).Schemas;
                fieldDataType = schemas[0].Tag;
            }

            switch (fieldDataType)
            {
                case Schema.Type.String:
                    return new System.String("").GetType();
                case Schema.Type.Int:
                    return new Int32().GetType();
                case Schema.Type.Float:
                    return new float().GetType();
                case Schema.Type.Double:
                    return new double().GetType();
                case Schema.Type.Logical:
                    return new byte[0].GetType();
                default:
                    return new System.String("").GetType();
            }
        }

        /// <summary>
        /// Converts data stored in a generic object to a C# primitive type
        /// </summary>
        /// <param name="fieldSchema"></param>
        /// <param name="fieldValue"></param>
        /// <returns></returns>
        private object GetTypedValue(Type dataType, object fieldValue)
        {
            if (dataType == typeof(Int32))
                return (int)fieldValue;
            if (dataType == typeof(float))
                return (float)fieldValue;
            if (dataType == typeof(float))
                return (double)fieldValue;
            if (dataType == typeof(byte[]))    
                return (byte[]) fieldValue;
            if (dataType == typeof(string))
                return (string)fieldValue;
            else
                return new Exception("Unsupported type");
        }

    }
}