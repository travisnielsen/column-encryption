using System.Text.Json.Serialization;

namespace ColumnEncrypt.Metadata
{
    /// <summary> Holds all data related to the column key </summary>
    public class ColumnKeyInfo
    {
        /// <summary> Column Key Name </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary> Encrypted blob of the column key </summary>
        [JsonPropertyName("encryptedColumnKey")]
        public string EncryptedColumnKey { get; set; }

        /// <summary> Encryption algorithm that was used to encrypt column key </summary>
        [JsonPropertyName("algorithm")]
        public string Algorithm { get; set; }

        /// <summary> Master key name that protects the column key </summary>
        [JsonPropertyName("columnMasterKeyName")]
        public string ColumnMasterKeyName { get; set; }
    }
}