namespace ColumnEncrypt.Metadata
{
    /// <summary> Holds information about the master key that protects the column key </summary>
    public class ColumnMasterKeyInfo
    {
        /// <summary> Master Key Name </summary>
        public string Name { get; set; }

        /// <summary> Master key provider </summary>
        public string KeyProvider { get; set; }

        /// <summary> Path or identifier of the master key </summary>
        public string KeyPath { get; set; }
    }
}
