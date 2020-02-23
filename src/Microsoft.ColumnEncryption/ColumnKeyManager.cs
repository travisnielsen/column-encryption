using Microsoft.ColumnEncryption.Common;
using Microsoft.ColumnEncryption.EncryptionProviders;
using Microsoft.ColumnEncryption.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Microsoft.ColumnEncryption
{
    public class ColumnKeyManager
    {
        /// <summary> Gets clear column keys either by generating new one or by decrypting already encrypted keys </summary>
        /// <param name="columnMasterKeyInfoList"> List of master key info </param>
        /// <param name="columnKeyInfoList"> List of column key info </param>
        /// <returns> Map of column key name to corresponding column key info and clear column key </returns>
        public static Dictionary<string, (ColumnKeyInfo keyInfo, ColumnKeyMaterial keyMaterial)> GetClearColumnKeys(
            IKeyProtectorFactory keyProtectorFactory,
            IEnumerable<ColumnMasterKeyInfo> columnMasterKeyInfoList,
            IEnumerable<ColumnKeyInfo> columnKeyInfoList)
        {
            Dictionary<string, (ColumnKeyInfo keyInfo, ColumnKeyMaterial keyMaterial)> result =
                new Dictionary<string, (ColumnKeyInfo keyInfo, ColumnKeyMaterial keyMaterial)>();

            foreach (ColumnKeyInfo columnKeyInfo in columnKeyInfoList)
            {
                ColumnKeyMaterial columnKeyMaterial = new ColumnKeyMaterial();
                ColumnMasterKeyInfo masterKeyInfo = columnMasterKeyInfoList.First(
                    c => c.Name.Equals(columnKeyInfo.ColumnMasterKeyName, StringComparison.InvariantCultureIgnoreCase));
                IKeyProtector keyProtector = keyProtectorFactory.Get(masterKeyInfo);

                if (columnKeyInfo.EncryptedColumnKey == null)
                {
                    columnKeyMaterial.ClearColumnKey = ColumnKeyManager.GenerateColumnKey();
                    byte[] encryptedColumnKeyBytes = keyProtector.EncryptColumnKey(masterKeyInfo.KeyPath, columnKeyInfo.Algorithm, columnKeyMaterial.ClearColumnKey);
                    columnKeyInfo.EncryptedColumnKey = Converter.ToHexString(encryptedColumnKeyBytes);
                }
                else
                {
                    columnKeyMaterial.ClearColumnKey = keyProtector.DecryptColumnKey(
                        masterKeyInfo.KeyPath,
                        columnKeyInfo.Algorithm,
                        Converter.FromHexString(columnKeyInfo.EncryptedColumnKey));
                }

                result[columnKeyInfo.Name] = (columnKeyInfo, columnKeyMaterial);
            }

            return result;
        }

        /// <summary> Generates a new AES key </summary>
        /// <returns> Generated AES key </returns>
        private static byte[] GenerateColumnKey()
        {
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            aes.KeySize = 256;
            aes.GenerateKey();

            return aes.Key;
        }
    }
}
