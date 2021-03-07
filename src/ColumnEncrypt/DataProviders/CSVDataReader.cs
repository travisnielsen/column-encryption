using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColumnEncrypt.Data;
using ColumnEncrypt.Metadata;
using Microsoft.Data.Encryption.FileEncryption;
using Microsoft.Data.Encryption.AzureKeyVaultProvider;
using Azure.Core;

namespace ColumnEncrypt.DataProviders
{
    /// <summary> Handles reading data from delimited files </summary>
    public class CSVDataReader : CSVData, IColumnarDataReader, IDisposable
    {
        private readonly CsvReader csvReader;

        public IList<FileEncryptionSettings> FileEncryptionSettings
        {
            get
            {
                return this.encryptionSettings;
            }
        }

        public string[] Header
        {
            get
            {
                return this.header;
            }
        }

        /// <summary> Initializes a new instance of <see cref="CSVDataReader"/> class with encryption metadata </summary>
        /// <param name="reader"> Text reader of the source </param>
        /// <param name="credential">A tokencredential for authenticating to Key Vault</param>
        /// <param name="encrypted">Indicates if the current file has encryption or not</param>
        public CSVDataReader(StreamReader reader, DataProtectionConfig config, TokenCredential credential, bool encrypted)
        {
            this.csvReader = new CsvReader(reader, true);
            this.encryptionSettings = new List<FileEncryptionSettings>();
            this.azureKeyProvider = new AzureKeyVaultKeyStoreProvider (credential);
            header = ReaderHeaderIfRequired();
            this.encryptionSettings = LoadFileEncryptionSettings(config, encrypted);
        }

        /// <summary> Initializes a new instance of <see cref="CSVDataReader"/> class </summary>
        /// <param name="reader"> Text reader of the source </param>
        public CSVDataReader(StreamReader reader)
        {
            this.csvReader = new CsvReader(reader, true);
            header = ReaderHeaderIfRequired();
        }

        public IEnumerable<IEnumerable<IColumn>> Read()
        {
            this.ReaderHeaderIfRequired();
            IEnumerable<ColumnData> columns = header.Select(n => new ColumnData(n)).ToArray();

            while (this.csvReader.Read())
            {
                var row = Enumerable.Range(0, columns.Count()).Select(i => this.csvReader.GetField(i)).ToArray();
                for (int i = 0; i < columns.Count(); i++)
                {
                    columns.ElementAt(i).Index = i;
                    columns.ElementAt(i).AddColumnRecord(row[i]);
                }
            }
            // var result = (IEnumerable<IEnumerable<IColumn>>)(columns as IColumn);
            var result = columns as IEnumerable<IColumn>;
            var result3 = new List<IEnumerable<IColumn>>();
            result3.Add(result);
            return result3;
        }

        private string[] ReaderHeaderIfRequired()
        {
            // NOT thread safe
            if (header != null) return header;
            this.csvReader.Read();
            this.csvReader.ReadHeader();
            return csvReader.Context.HeaderRecord;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.csvReader?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CSVDataReader()
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

        #endregion
    }
}