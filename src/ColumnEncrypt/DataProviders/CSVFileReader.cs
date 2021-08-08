using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using ColumnEncrypt.Data;
using ColumnEncrypt.Metadata;
using ColumnEncrypt.Util;
using Microsoft.Data.Encryption.FileEncryption;

namespace ColumnEncrypt.DataProviders
{
    /// <summary> Handles reading data from delimited files </summary>
    public class CSVFileReader : IColumnarDataReader, IDisposable
    {
        private readonly CsvReader csvReader;
        private IList<FileEncryptionSettings> encryptionSettings;
        private string[] header;

        public IList<FileEncryptionSettings> FileEncryptionSettings
        {
            get { return this.encryptionSettings; }
            set { encryptionSettings = value; }
        }

        public string[] Header
        {
            get
            {
                return this.ReaderHeaderIfRequired();
            }
        }

        /// <summary> Initializes a new instance of <see cref="CSVDataReader"/> class </summary>
        /// <param name="reader">source csv file</param>
        /// <param name="settings">column encryption settings</param>
        public CSVFileReader(StreamReader reader)
        {
            this.csvReader = new CsvReader(reader, CultureInfo.InvariantCulture, true);
            header = ReaderHeaderIfRequired();
        }

        public IEnumerable<IEnumerable<IColumn>> Read()
        {
            if (encryptionSettings == null)
            {
                throw new Exception("Cannot read CSV without encryption settings");
            }

            List<ColumnData> columns = new List<ColumnData>();

            for (int i=0; i < header.Length; i++)
            {
                var columnEncryptionSetting = encryptionSettings[i];

                // If the source column is encrypted, we need to set the column data type to byte[] by passing the type into the ColumnData constructor
                if (columnEncryptionSetting.EncryptionType != Microsoft.Data.Encryption.Cryptography.EncryptionType.Plaintext)
                {
                    columns.Add(new ColumnData(header[i], typeof(byte[])));
                }
                // othereise, we'll default to string
                else
                {
                    columns.Add(new ColumnData(header[i], typeof(string)));
                }
            }

            while (this.csvReader.Read())
            {
                var row = Enumerable.Range(0, columns.Count()).Select(i => this.csvReader.GetField(i)).ToArray();
                for (int i = 0; i < columns.Count(); i++)
                {
                    columns.ElementAt(i).Index = i;
                    var columnEncryptionSetting = encryptionSettings[i];

                    Type columnDataType = columns.ElementAt(i).DataType;

                    // if (columnEncryptionSetting.EncryptionType != Microsoft.Data.Encryption.Cryptography.EncryptionType.Plaintext)
                    if (typeof(byte[]) == columnDataType)
                    {
                        // Column encrypted. Need to convert the hexstring to a byte array for decryption
                        byte[] columnData = Converter.FromHexString(row[i]);
                        columns.ElementAt(i).AddColumnRecord(columnData);
                    }
                    else
                    {
                        columns.ElementAt(i).AddColumnRecord(row[i]);
                    }
                }
            }

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
            return csvReader.HeaderRecord;
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