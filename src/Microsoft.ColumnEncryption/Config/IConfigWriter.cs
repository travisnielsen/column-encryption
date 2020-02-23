using Microsoft.ColumnEncryption.Metadata;
using System.IO;

namespace Microsoft.ColumnEncryption.Config
{
    /// <summary>
    /// Configuration provider contracts that define 
    /// how the column data should be encrypted or decrypted
    /// </summary>
    public interface IConfigWriter
    {
        /// <summary>
        /// Persists column configuration data to the store.
        /// In case of new encryption keys getting generated, this write will
        /// persist new config into the store.
        /// NOTE: The write method is not expected to check for changes to the config
        /// data. It will perform a blind write.
        /// </summary>
        /// <param name="currentConfig"> Column configuration data </param>
        void Write(DataProtectionConfig currentConfig);
    }
}
