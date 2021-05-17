using System;
using System.Collections.Generic;
using Microsoft.Data.Encryption.FileEncryption;

namespace ColumnEncrypt.DataProviders
{
    public class AvroDataWriter : IColumnarDataWriter, IDisposable
    {
        public IList<FileEncryptionSettings> FileEncryptionSettings => throw new System.NotImplementedException();

        public void Write(IEnumerable<IColumn> columns)
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}