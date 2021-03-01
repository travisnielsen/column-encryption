namespace ColumnEncryption.Util.Metadata
{
    /// <summary> 
    /// Holds column key in the clear. 
    /// NOTE: This should never get persisted
    /// </summary>
    public class ColumnKeyMaterial
    {
        /// <summary> Column key in clear - a symmetric key </summary>
        public byte[] ClearColumnKey { get; set; }
    }
}
