using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Encryption.FileEncryption;

using Avro;
using Avro.IO;
using Avro.Generic;
using Avro.File;
using ColumnEncrypt.Data;

namespace ColumnEncrypt.DataProviders
{
    public class AvroDataReader : IColumnarDataReader, IDisposable
    {
        private Stream _stream;
        private Schema _schema;
        public IList<FileEncryptionSettings> FileEncryptionSettings => throw new System.NotImplementedException();

        public AvroDataReader(Stream stream, string schemaJson)
        {
            _stream = stream;

            try
            {
                _schema = Schema.Parse(schemaJson);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public IEnumerable<IEnumerable<IColumn>> Read()
        {
            var columnData = new List<ColumnData>();
            var reader = DataFileReader<GenericRecord>.OpenReader(_stream, _schema);
            
            while (reader.HasNext())
            {
                // TODO: logic here
            }

            var columnDataEnum = columnData as IEnumerable<IColumn>;
            var result = new List<IEnumerable<IColumn>>();
            result.Add(columnDataEnum);
            return result;

        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}