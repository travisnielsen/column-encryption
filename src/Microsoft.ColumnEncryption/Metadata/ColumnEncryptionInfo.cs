using Microsoft.Data.SqlClient;

namespace Microsoft.ColumnEncryption.Metadata
{
    /// <summary> Holds all encryption related information for a column </summary>
    public class ColumnEncryptionInfo
    {
        /// <summary> Column Name </summary>
        public string ColumnName { get; set; }

        /// <summary> Key name that protects column data </summary>
        public string ColumnKeyName { get; set; }

        /// <summary> Type of encryption to be performed on the data 1 - Deterministic, 2 - Random </summary>
        public SqlClientEncryptionType EncryptionType { get; set; }

        /// <summary> Algorithm to be used to encrypt column data </summary>
        public string Algorithm { get; set; }
    }
}