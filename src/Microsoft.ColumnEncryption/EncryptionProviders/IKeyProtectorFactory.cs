using Microsoft.ColumnEncryption.Metadata;

namespace Microsoft.ColumnEncryption.EncryptionProviders
{
    /// <summary> Factory interface to instantiate the right key protector </summary>
    public interface IKeyProtectorFactory
    {
        /// <summary> Gets the right key protector object based on the master key config </summary>
        /// <param name="columnMasterKeyInfo"> Master key configuration </param>
        /// <returns> Key protector object </returns>
        IKeyProtector Get(ColumnMasterKeyInfo columnMasterKeyInfo);
    }
}