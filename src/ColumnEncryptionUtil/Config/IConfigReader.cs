using ColumnEncryption.Util.Metadata;

namespace ColumnEncryption.Util.Config
{
    /// <summary>
    /// Configuration provider contracts that define 
    /// how the column data should be encrypted or decrypted
    /// </summary>
    public interface IConfigReader
    {
        /// <summary> 
        /// Reads information about column names and corresponding
        /// encryption policies to be applied from the config store.
        /// </summary>
        /// <param name="reader"> Reader for config source </param>
        /// <returns> Column encryption configuration data </returns>
        DataProtectionConfig Read();
    }
}
