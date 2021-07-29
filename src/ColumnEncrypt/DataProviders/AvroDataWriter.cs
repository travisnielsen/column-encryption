using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Encryption.FileEncryption;
using Avro;
using Avro.File;
using Avro.Util;
using Avro.Generic;
using ColumnEncrypt.Data;
using ColumnEncrypt.Metadata;

namespace ColumnEncrypt.DataProviders
{
    public class AvroDataWriter : IColumnarDataWriter, IDisposable
    {
        private StreamWriter fileWriteStream;
        private Schema avroSchema;
        public IList<FileEncryptionSettings> encryptionSettings;
        public LogicalTypeFactory logicalTypeFactory = LogicalTypeFactory.Instance;

        public IList<FileEncryptionSettings> FileEncryptionSettings
        {
            get
            {
                return this.encryptionSettings;
            }
        }

        /// <summary> Initializes a new instances of <see cref="AvroDataWriter"/> class </summary>
        /// <param name="writer">Text writer to the destination file</param>
        /// <param name="settings">Text writer to the destination file</param>
        /// <param name="avroSchema">serialized JSON scheme representing the document schema</param>
        public AvroDataWriter(StreamWriter writer, IList<FileEncryptionSettings> settings, string schema)
        {
            this.fileWriteStream = writer;
            this.encryptionSettings = settings;
            logicalTypeFactory.Register(new EncryptedLogicalType());

            if (schema != null)
            {
                avroSchema = Avro.Schema.Parse(schema);
            }
        }

        public void Write(IEnumerable<IColumn> columns)
        {
            RecordSchema recordSchema = (RecordSchema)Schema.Parse(avroSchema.ToString());

            // Create the metadata for encryption
            DataProtectionConfig dataProtectionConfig = CreateEncryptionMetadata(recordSchema.Fields);

            IList<GenericRecord> records = CreateRecords(columns, recordSchema);
            DatumWriter<GenericRecord> genericDatumWriter = new GenericDatumWriter<GenericRecord>(recordSchema);

            using (var writer = DataFileWriter<GenericRecord>.OpenWriter(genericDatumWriter, fileWriteStream.BaseStream))
            {
                // Serialize crypto metadata
                string columnKeyMetadata = JsonSerializer.Serialize<List<ColumnKeyInfo>>(dataProtectionConfig.ColumnKeyInfo);
                string columnMasterKeyMetadata = JsonSerializer.Serialize<List<ColumnMasterKeyInfo>>(dataProtectionConfig.ColumnMasterKeyInfo);
                writer.SetMeta("columnKeyInfo", columnKeyMetadata);
                writer.SetMeta("columnMasterKeyInfo", columnMasterKeyMetadata);

                foreach (var record in records)
                {
                    writer.Append(record);
                }
            }
        }

        public void Dispose()
        {
            // TODO: Evaluate proper approach for disposal
            // throw new NotImplementedException();
        }

        private IList<GenericRecord> CreateRecords(IEnumerable<IColumn> columns, RecordSchema recordSchema)
        {
            List<GenericRecord> records = new List<GenericRecord>();
            var recordCount = columns?.FirstOrDefault()?.Data.Length;
            
            for (int i = 0; i < recordCount; i++)
            {
                GenericRecord record = new GenericRecord(recordSchema);

                foreach (var column in columns)
                {
                    string fieldName = column.Name;
                    object fieldValue = column.Data.GetValue(i);
                    Schema fieldSchema = recordSchema[fieldName].Schema;

                    if (fieldSchema.Tag == Schema.Type.Union)
                    {
                        var schemas = ((Avro.UnionSchema)fieldSchema).Schemas;
                        fieldValue = ConvertData(schemas[0], fieldValue);
                    }
                    else if (fieldSchema is EnumSchema)
                    {
                        GenericEnum ge = new GenericEnum(fieldSchema as EnumSchema, (string)fieldValue);
                        fieldValue = ge;
                    }
                    else if (fieldSchema is FixedSchema)
                    {
                        GenericFixed gf = new GenericFixed(fieldSchema as FixedSchema);
                        gf.Value = (byte[])fieldValue;
                        fieldValue = gf;
                    }
                    else
                    {
                        fieldValue = ConvertData(fieldSchema, fieldValue);
                    }

                    record.Add(fieldName, fieldValue);
                }

                records.Add(record);
            }

            return records;
        }

        private object ConvertData(Schema schema, object value)
        {
            /*
            if(value.GetType().Name == "Byte[]")
            {
                return ColumnEncrypt.Util.Converter.ToHexString((byte[]) value);
            }
            */

            switch (schema.Tag)
            {
                case Schema.Type.Int:
                    return Int32.Parse(value.ToString());
                case Schema.Type.Float:
                    return float.Parse(value.ToString());
                case Schema.Type.Double:
                    return double.Parse(value.ToString());
                case Schema.Type.Boolean:
                    return bool.Parse(value.ToString());
                default:
                    return value;
            }
        }


        private DataProtectionConfig CreateEncryptionMetadata(List<Field> fields)
        {
            DataProtectionConfig metadata = new DataProtectionConfig();

            for (int i = 0; i < fields.Count; i++)
            {
                var fieldCryptoConfig = encryptionSettings[i];

                if (fieldCryptoConfig.EncryptionType != Microsoft.Data.Encryption.Cryptography.EncryptionType.Plaintext)
                {
                    metadata.ColumnEncryptionInfo.Add( new ColumnEncryptionInfo
                        {
                            Algorithm = "AEAD_AES_256_CBC_HMAC_SHA256",
                            ColumnKeyName = fieldCryptoConfig.DataEncryptionKey.Name,
                            ColumnName = fields[i].Name,
                            EncryptionType = fieldCryptoConfig.EncryptionType.ToString()
                        });

                    metadata.ColumnKeyInfo.Add(new ColumnKeyInfo
                        {
                            Algorithm = "RSA_OAEP",
                            Name = fieldCryptoConfig.DataEncryptionKey.Name,
                            ColumnMasterKeyName = fieldCryptoConfig.DataEncryptionKey.KeyEncryptionKey.Name,
                            EncryptedColumnKey = Util.Converter.ToHexString(fieldCryptoConfig.DataEncryptionKey.EncryptedValue)
                        });

                    metadata.ColumnMasterKeyInfo.Add( new ColumnMasterKeyInfo 
                        {
                            Name = fieldCryptoConfig.DataEncryptionKey.KeyEncryptionKey.Name,
                            KeyProvider = fieldCryptoConfig.DataEncryptionKey.KeyEncryptionKey.KeyStoreProvider.ProviderName,
                            KeyPath = fieldCryptoConfig.DataEncryptionKey.KeyEncryptionKey.Path
                        });
                }

                // metadata.ColumnKeyInfo = (List<ColumnKeyInfo>)metadata.ColumnKeyInfo.Distinct();
                // metadata.ColumnMasterKeyInfo = (List<ColumnMasterKeyInfo>)metadata.ColumnMasterKeyInfo.Distinct();

                // Gets the type info from the Avro schema for that field. Example: "{\"type\":\"int\"}"
                // string fieldSchema = field.Schema.ToString();

                // This will return any custom property set for a logical type
                // string encryptedProperty = field.Schema.GetProperty("columnKeyName");

            }

            return metadata;

        }
    }
}