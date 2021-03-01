using System;
using System.Collections.Generic;
using System.Text;

namespace ColumnEncryption.Util.Metadata
{
    /// <summary> Config to determine how data is to be protected </summary>
    public class DataProtectionConfig
    {
        /// <summary> List of columns to be encrypted and with which key </summary>
        public List<ColumnEncryptionInfo> ColumnEncryptionInfo { get; set; }

        /// <summary> Column key information to encrypt column data </summary>
        public List<ColumnKeyInfo> ColumnKeyInfo { get; set; }

        /// <summary> Master key information to encrypt column key </summary>
        public List<ColumnMasterKeyInfo> ColumnMasterKeyInfo { get; set; }
    }
}
