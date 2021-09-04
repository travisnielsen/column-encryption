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
    public class AvroFileReader : IColumnarDataReader, IDisposable
    {
        private Stream _fileReaderStream;
        private IList<FileEncryptionSettings> _fileEncryptionSettings;
        private IDictionary<string, EncryptionKeyStoreProvider> _encryptionKeyStoreProviders;
        private Dictionary<string, Type> _fields;
        private LogicalTypeFactory _logicalTypeFactory = LogicalTypeFactory.Instance;

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
                if (_fields.Count == 0)
                {
                    bool result = LoadFileMetadata();
                    return _fields.Keys.ToArray();
                }
                else
                {
                    return _fields.Keys.ToArray();
                }
            }
        }

        public AvroFileReader(Stream readerStream, IDictionary<string, EncryptionKeyStoreProvider> encryptionKeyStoreProviders)
        {
            _fileReaderStream = readerStream;
            _encryptionKeyStoreProviders = encryptionKeyStoreProviders;
            _logicalTypeFactory.Register(new EncryptedLogicalTypeFile());
            _fields = new Dictionary<string, Type>();
        }

        /// <summary>
        /// Reads data from an Avro file
        /// </summary>
        /// <returns>List of columns with each column having a list of data elements</returns>
        public IEnumerable<IEnumerable<IColumn>> Read()
        {
            // TODO: Previous reads update the stream position so subsequent usses of the same stream need to re-set to zero. There's probabaly a better way to do this.
            _fileReaderStream.Position = 0;

            var columns = new List<ColumnData>();

            using (var reader = DataFileReader<GenericRecord>.OpenReader(_fileReaderStream, false))
            {
                int i = 0;
                foreach(var item in _fields)
                {
                    ColumnData newColumn = new ColumnData(item.Key, item.Value);
                    newColumn.Index = i;
                    columns.Add(newColumn);
                    i++;
                }

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

        public void Dispose()
        {
            // TODO: Determine right way to dispose the Avro reader
            // throw new NotImplementedException();
        }

        private bool LoadFileMetadata()
        {
            _fileEncryptionSettings = new List<FileEncryptionSettings>();

            using (var reader = DataFileReader<GenericRecord>.OpenReader(_fileReaderStream, true))
            {
                RecordSchema schema = (RecordSchema)reader.GetSchema();
                _fields = GetFieldInfo(schema, reader);

                // Load encryption metadata from the Avro document
                List<ColumnKeyInfo> columnKeyInfo = JsonSerializer.Deserialize<List<ColumnKeyInfo>>(reader.GetMetaString("columnKeyInfo"));
                List<ColumnMasterKeyInfo> masterKeyInfo = JsonSerializer.Deserialize<List<ColumnMasterKeyInfo>>(reader.GetMetaString("columnMasterKeyInfo"));

                // Set a default key from config. This is required by the MDE SDK for columns that are not part of encryption
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

                    _fileEncryptionSettings.Add(columnEncryptionSettings);
                }
            }

            return true;
        }

        /// <summary>
        /// Uses the Avro schema to provide the name and type of each field
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="reader"></param>
        /// <returns>Dictionary of field names and associated C# types</returns>
        private Dictionary<string, Type> GetFieldInfo(RecordSchema schema, IFileReader<GenericRecord> reader)
        {
            Dictionary<string, Type> fields = new Dictionary<string, Type>();

            foreach (var field in schema.Fields)
            {
                fields.Add(field.Name, GetFieldDataType(field.Schema));
            }

            return fields;
        }

        /// <summary>
        /// Returns the C# type of an Avro field. For Union fields, the first Avro data type excluding 'null' is used 
        /// </summary>
        /// <param name="fieldSchema"></param>
        /// <returns>C# type</returns>
        private Type GetFieldDataType(Schema fieldSchema)
        {
            Schema.Type fieldDataType = fieldSchema.Tag;

            if (fieldDataType == Schema.Type.Union)
            {
                var schemas = ((Avro.UnionSchema)fieldSchema).Schemas;
                fieldDataType = schemas.Where(s => s.Name != "null").FirstOrDefault().Tag;
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