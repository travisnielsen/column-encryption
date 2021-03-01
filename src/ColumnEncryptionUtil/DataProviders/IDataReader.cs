using ColumnEncryption.Util.Data;
using System.Collections.Generic;

namespace ColumnEncryption.Util.DataProviders
{
    /// <summary> Interface definition for methods to support reading from data sources </summary>
    public interface IDataReader
    {
        /// <summary> Reads data from source </summary>
        /// <returns> Column wise data </returns>
        IEnumerable<ColumnData> Read();
    }
}