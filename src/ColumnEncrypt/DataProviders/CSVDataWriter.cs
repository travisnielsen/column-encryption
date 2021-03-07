using CsvHelper;
using ColumnEncrypt.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Encryption.FileEncryption;
using Azure.Core;
using Microsoft.Data.Encryption.Cryptography;
using Microsoft.Data.Encryption.AzureKeyVaultProvider;
using ColumnEncrypt.Metadata;

namespace ColumnEncrypt.DataProviders
{
    /// <summary> Handles writing data to delimited files </summary>
    public class CSVDataWriter : CSVData, IColumnarDataWriter, IDisposable
    {
        private readonly CsvWriter csvWriter;

        public IList<FileEncryptionSettings> FileEncryptionSettings
        {
            get
            {
                return this.encryptionSettings;
            }
        }

        /// <summary> Initializes a new instances of <see cref="CSVDataWriter"/> class </summary>
        /// <param name="writer"> Text writer to the destination file </param>
        public CSVDataWriter(StreamWriter writer, DataProtectionConfig config, TokenCredential credential, string[] header, bool encrypted)
        {
            this.csvWriter = new CsvWriter(writer);
            this.header = header;
            this.azureKeyProvider = new AzureKeyVaultKeyStoreProvider (credential);
            this.encryptionSettings = LoadFileEncryptionSettings(config, encrypted);
        }

        /// <inheritdoc/>
        public void Write(IEnumerable<IColumn> columnData)
        {
            var headers = columnData.Select(c => c.Name).ToList();

            headers.ForEach(h => this.csvWriter.WriteField(h));
            this.csvWriter.NextRecord();

            var recordCount = columnData?.FirstOrDefault()?.Data.Length;

            for (int i = 0; i < recordCount; i++)
            {
                foreach (var column in columnData)
                {
                    this.csvWriter.WriteField(column.Data.GetValue(i));
                }
                this.csvWriter.NextRecord();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.csvWriter?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CSVDataWriter()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        # endregion

    }
}