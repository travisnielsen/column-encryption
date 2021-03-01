﻿using ColumnEncryption.Util.Data;
using System.Collections.Generic;

namespace ColumnEncryption.Util.DataProviders
{
    /// <summary> Interface definition for methods to support writing to data targets </summary>
    public interface IDataWriter
    {
        /// <summary> Writes provided column wise data to the data sink </summary>
        /// <param name="columnData"> Column wise data </param>
        void Write(IEnumerable<ColumnData> columnData);
    }
}