using System.Text.Json.Serialization;

namespace ColumnEncrypt.Metadata
{
    /// <summary> Holds information about the master key that protects the column key </summary>
    public class ColumnMasterKeyInfo
    {
        /// <summary> Master Key Name </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary> Master key provider </summary>
        [JsonPropertyName("keyProvider")]
        public string KeyProvider { get; set; }

        /// <summary> Path or identifier of the master key </summary>
        [JsonPropertyName("keyPath")]
        public string KeyPath { get; set; }
    }
}
