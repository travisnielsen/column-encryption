using CsvHelper;
using Microsoft.ColumnEncryption.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.ColumnEncryption.DataProviders
{
    /// <summary> Handles writing data to delimited files </summary>
    public class CSVDataWriter : IDataWriter, IDisposable
    {
        private readonly CsvWriter csvWriter;

        /// <summary> Initializes a new instances of <see cref="CSVDataWriter"/> class </summary>
        /// <param name="writer"> Text writer to the destination file </param>
        public CSVDataWriter(StreamWriter writer)
        {
            this.csvWriter = new CsvWriter(writer);
        }

        /// <inheritdoc/>
        public void Write(IEnumerable<ColumnData> columnData)
        {
            var headers = columnData.Select(c => c.Name).ToList();

            headers.ForEach(h => this.csvWriter.WriteField(h));
            this.csvWriter.NextRecord();

            var recordCount = columnData?.FirstOrDefault()?.Data?.Count;

            for (int i = 0; i < recordCount; i++)
            {
                foreach (var column in columnData)
                {
                    this.csvWriter.WriteField(column.Data[i]);
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
        #endregion
    }
}