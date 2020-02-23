using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;

namespace Microsoft.ColumnEncryption.EncryptionProviders
{
    public class AlwaysEncryptedDataProtector : IDataProtector
    {
        private readonly SqlAeadAes256CbcHmac256Algorithm cryptoAlg;

        public AlwaysEncryptedDataProtector(
            byte[] columnEncryptionKey, 
            SqlClientEncryptionType encryptionType,
            string encryptionAlgorithm)
        {
            if (columnEncryptionKey == null || columnEncryptionKey.Length == 0) throw new ArgumentNullException(nameof(columnEncryptionKey));
            if (string.IsNullOrWhiteSpace(encryptionAlgorithm)) throw new ArgumentNullException(nameof(encryptionAlgorithm));

            SqlAeadAes256CbcHmac256EncryptionKey sqlCryptoKey = new SqlAeadAes256CbcHmac256EncryptionKey(
                columnEncryptionKey, 
                encryptionAlgorithm);

            this.cryptoAlg = new SqlAeadAes256CbcHmac256Algorithm(sqlCryptoKey, encryptionType, 1);
        }

        public IEnumerable<byte[]> Decrypt(IEnumerable<byte[]> encryptedData)
        {
            return encryptedData.Select(d => cryptoAlg.DecryptData(d)).ToList();
        }

        public IEnumerable<byte[]> Encrypt(IEnumerable<byte[]> unencryptedData)
        {
            return unencryptedData.Select(d => cryptoAlg.EncryptData(d)).ToList();
        }
    }
}