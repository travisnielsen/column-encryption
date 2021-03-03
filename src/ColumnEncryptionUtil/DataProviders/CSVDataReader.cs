﻿using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColumnEncryption.Util.Data;
using Microsoft.Data.Encryption.FileEncryption;

namespace ColumnEncryption.Util.DataProviders
{
    /// <summary> Handles reading data from delimited files </summary>
    public class CSVDataReader : IColumnarDataReader, IDisposable
    {
        private readonly CsvReader csvReader;

        private string[] header;

        /// <summary> Initializes a new instance of <see cref="CSVDataReader"/> class </summary>
        /// <param name="reader"> Text reader of the source </param>
        public CSVDataReader(StreamReader reader)
        {
            this.csvReader = new CsvReader(reader, true);
        }

        /// <inheritdoc/>
        /*
        public IEnumerable<ColumnData> Read()
        {
            this.ReaderHeaderIfRequired();
            var columns = header.Select(n => new ColumnData(n)).ToArray();

            while (this.csvReader.Read())
            {
                var row = Enumerable.Range(0, columns.Length).Select(i => this.csvReader.GetField(i)).ToArray();
                for (int i = 0; i < columns.Length; i++)
                {
                    columns[i].Index = i;
                    columns[i].DataList.Add(row[i]);
                }
            }

            return columns;
        }
        */

        private void ReaderHeaderIfRequired()
        {
            // NOT thread safe
            if (this.header != null) return;

            this.csvReader.Read();
            this.csvReader.ReadHeader();
            this.header = csvReader.Context.HeaderRecord;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public IList<FileEncryptionSettings> FileEncryptionSettings => throw new NotImplementedException();

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
                    columns.ElementAt(i).DataList.Add(row[i]);
                }
            }

            // TODO: I'm sure there's a much better way to handle this
            foreach (ColumnData column in columns)
            {
                column.Data = column.DataList.ToArray();
                column.DataList = null;
            }

            return (IEnumerable<IEnumerable<IColumn>>)columns;
            
        }
        #endregion
    }
}