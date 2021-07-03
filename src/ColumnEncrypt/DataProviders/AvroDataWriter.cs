using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Encryption.FileEncryption;
using Avro;
using Avro.File;
using Avro.Generic;
using System.Linq;

namespace ColumnEncrypt.DataProviders
{
    public class AvroDataWriter : IColumnarDataWriter, IDisposable
    {
        private Schema schema;
        public IList<FileEncryptionSettings> encryptionSettings;

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
            this.encryptionSettings = settings;
            schema = Avro.Schema.Parse(avroSchema);
        }

        public void Write(IEnumerable<IColumn> columns)
        {
            RecordSchema rs = (RecordSchema)Schema.Parse(schema.ToString());
            IList<GenericRecord> records = createRecords(columns, rs);
            DatumWriter<GenericRecord> genericDatumWriter = new GenericDatumWriter<GenericRecord>(schema);

            using (var writer = DataFileWriter<GenericRecord>.OpenAppendWriter(genericDatumWriter, "test.avro"))
            {
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

        private static IList<GenericRecord> createRecords(IEnumerable<IColumn> columns, RecordSchema recordSchema)
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
                    Schema inner = recordSchema[fieldName].Schema;

                    if (inner is EnumSchema)
                    {
                        GenericEnum ge = new GenericEnum(inner as EnumSchema, (string)fieldValue);
                        fieldValue = ge;
                    }
                    else if (inner is FixedSchema)
                    {
                        GenericFixed gf = new GenericFixed(inner as FixedSchema);
                        gf.Value = (byte[])fieldValue;
                        fieldValue = gf;
                    }

                    record.Add(fieldName, fieldValue);
                }

                records.Add(record);
            }

            return records;
            
        }

    }
}