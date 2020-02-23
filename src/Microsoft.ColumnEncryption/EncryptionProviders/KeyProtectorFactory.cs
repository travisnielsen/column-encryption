using Microsoft.ColumnEncryption.Auth;
using Microsoft.ColumnEncryption.Common;
using Microsoft.ColumnEncryption.Metadata;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Microsoft.ColumnEncryption.EncryptionProviders
{
    /// <summary> Factory class to retrieve right key protector </summary>
    public class KeyProtectorFactory : IKeyProtectorFactory
    {
        /// <summary> Application settings </summary>
        private readonly Settings settings;
        private readonly IAuthProvider authProvider;

        /// <summary> Cache to hold and reuse keyprotector object instances </summary>
        private Dictionary<string, IKeyProtector> KeyProtectors;

        /// <summary> Initializes a new instance of <see cref="KeyProtectorFactory"/> class </summary>
        /// <param name="settings"> Application settings </param>
        public KeyProtectorFactory(Settings settings, IAuthProvider authProvider)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (authProvider == null) throw new ArgumentNullException(nameof(authProvider));

            this.KeyProtectors = new Dictionary<string, IKeyProtector>();
            this.settings = settings;
            this.authProvider = authProvider;
        }

        /// <summary> 
        /// Gets the right key protector object instance based on the master key info.
        /// Uses a cache to avoid creating objects of the same type again.
        /// </summary>
        /// <param name="columnMasterKeyInfo"> Master key information </param>
        /// <returns> Key protector object </returns>
        public IKeyProtector Get(ColumnMasterKeyInfo columnMasterKeyInfo)
        {
            if (columnMasterKeyInfo == null) throw new ArgumentNullException(nameof(columnMasterKeyInfo));

            switch (columnMasterKeyInfo.KeyProvider)
            {
                case "AZURE_KEY_VAULT":
                    if (!this.KeyProtectors.ContainsKey(columnMasterKeyInfo.KeyProvider))
                    {
                        // TODO: Pass in callback
                        this.KeyProtectors.Add(columnMasterKeyInfo.KeyProvider, new AKVKeyProtector(this.authProvider));
                    }
                    break;

                /*
                case "AZURE_INFORMATION_PROTECTION":
                    if (!this.KeyProtectors.ContainsKey(columnMasterKeyInfo.KeyProvider))
                    {
                        this.KeyProtectors.Add(columnMasterKeyInfo.KeyProvider, new AIPKeyProtector(this.settings, this.authProvider));
                    }
                    break;
                */

                default:
                    throw new Exception($"Unsupported key provider {columnMasterKeyInfo.KeyProvider} for key {columnMasterKeyInfo.Name}");
            }

            return this.KeyProtectors[columnMasterKeyInfo.KeyProvider];
        }
    }
}