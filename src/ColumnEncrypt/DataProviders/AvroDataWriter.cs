using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Encryption.FileEncryption;
using Avro;
using Avro.File;
using Avro.Util;
using Avro.Generic;
using ColumnEncrypt.Data;

namespace ColumnEncrypt.DataProviders
{
    public class AvroDataWriter : IColumnarDataWriter, IDisposable
    {
        private StreamWriter fileWriteStream;
        private Schema schema;
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
        public AvroDataWriter(StreamWriter writer, IList<FileEncryptionSettings> settings, string avroSchema)
        {
            this.fileWriteStream = writer;
            this.encryptionSettings = settings;
            logicalTypeFactory.Register(new EncryptedLogicalType());
            schema = Avro.Schema.Parse(avroSchema);
        }

        public void Write(IEnumerable<IColumn> columns)
        {
            RecordSchema rs = (RecordSchema)Schema.Parse(schema.ToString());
            IList<GenericRecord> records = createRecords(columns, rs);
            DatumWriter<GenericRecord> genericDatumWriter = new GenericDatumWriter<GenericRecord>(schema);

            using (var writer = DataFileWriter<GenericRecord>.OpenWriter(genericDatumWriter, fileWriteStream.BaseStream))
            {
                foreach (var record in records)
                {
                    writer.Append(record);
                }

                // writer.SetMeta()
            }
        }

        public void Dispose()
        {
            // TODO: Evaluate proper approach for disposal
            // throw new NotImplementedException();
        }

        private IList<GenericRecord> createRecords(IEnumerable<IColumn> columns, RecordSchema recordSchema)
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
                        fieldValue = convertData(schemas[0], fieldValue);
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
                        fieldValue = convertData(fieldSchema, fieldValue);
                    }

                    record.Add(fieldName, fieldValue);
                }

                records.Add(record);
            }

            return records;
            
        }

        private object convertData(Schema schema, object value)
        {
            // TODO: Use Logical Types for encrypted data
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

    }
}