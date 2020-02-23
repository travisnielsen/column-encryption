using Microsoft.Azure.KeyVault;
using Microsoft.ColumnEncryption.Auth;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using System;

namespace AlwaysProtected.App.EncryptionProviders
{
    /// <summary> Protects column keys using master keys in Azure Key Vault </summary>
    public class AKVKeyProtector : IKeyProtector
    {
        /// <summary> SQL AE key vault master key store provider </summary>
        private readonly SqlColumnEncryptionAzureKeyVaultProvider provider;

        /// <summary> Initializes a new instance of <see cref="AKVKeyProtector"/> class </summary>
        /// <param name="authenticationCallback"> Auth callback </param>
        public AKVKeyProtector(IAuthProvider authProvider)
        {
            if (authProvider == null) throw new ArgumentNullException(nameof(authProvider));

            this.provider = new SqlColumnEncryptionAzureKeyVaultProvider(authProvider.AcquireTokenAsync);
        }

        /// <summary> Encrypt clear column key using specified master key path and algorithm </summary>
        /// <param name="masterKeyPath"> Master key path </param>
        /// <param name="algorithm"> Algorithm to use when encrypting column key </param>
        /// <param name="clearColumnKey"> Column key in clear </param>
        /// <returns> Encrypted column key </returns>
        public byte[] EncryptColumnKey(string keyPath, string algorithm, byte[] clearColumnKey)
        {
            return this.provider.EncryptColumnEncryptionKey(keyPath, algorithm, clearColumnKey);
        }

        /// <summary> Decrypt encrypted column key </summary>
        /// <param name="masterKeyPath"> Master key path </param>
        /// <param name="algorithm"> Algorithm to use when encrypting column key </param>
        /// <param name="encryptedColumnKey"> Encrypted column key </param>
        /// <returns> Decrypted column key </returns>
        public byte[] DecryptColumnKey(string keyPath, string algorithm, byte[] encryptedColumnKey)
        {
            return this.provider.DecryptColumnEncryptionKey(keyPath, algorithm, encryptedColumnKey);
        }
    }
}