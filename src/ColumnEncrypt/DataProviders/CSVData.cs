using System.Collections.Generic;
using Microsoft.Data.Encryption.Cryptography;
using Microsoft.Data.Encryption.FileEncryption;

namespace ColumnEncrypt.DataProviders
{
    public class CSVData
    {
        protected string[] header;
        protected EncryptionKeyStoreProvider azureKeyProvider;
        protected KeyEncryptionKey defaultKEK;
        protected IList<FileEncryptionSettings> encryptionSettings;
        protected bool isEncrypted = false;

    }
}