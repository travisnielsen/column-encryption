using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using Microsoft.Data.Encryption.FileEncryption;
using Azure.Core;
using Microsoft.Data.Encryption.AzureKeyVaultProvider;
using ColumnEncrypt.Metadata;
using ColumnEncrypt.Util;

namespace ColumnEncrypt.DataProviders
{
    /// <summary> Handles writing data to delimited files </summary>
    public class CSVDataWriter : IColumnarDataWriter, IDisposable
    {
        private readonly CsvWriter csvWriter;
        private IList<FileEncryptionSettings> encryptionSettings;
        private string[] header;

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
            this.csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
            this.header = header;
            this.encryptionSettings = ColumnSettings.Load(config, header, new AzureKeyVaultKeyStoreProvider(credential), encrypted);
        }

        /// <inheritdoc/>
        public void Write(IEnumerable<IColumn> columnData)
        {
            var headers = columnData.Select(c => c.Name).ToList();
            headers.ForEach(h => csvWriter.WriteField(h));
            this.csvWriter.NextRecord();

            var recordCount = columnData?.FirstOrDefault()?.Data.Length;

            for (int i = 0; i < recordCount; i++)
            {
                foreach (var column in columnData)
                {
                    csvWriter.WriteField(column.Data.GetValue(i));
                }
                csvWriter.NextRecord();
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
                    csvWriter?.Dispose();
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