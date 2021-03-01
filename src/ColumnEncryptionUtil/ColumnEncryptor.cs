/*
using ColumnEncryption.Util.Auth;
using ColumnEncryption.Util.Common;
using Microsoft.ColumnEncryption.Config;
using Microsoft.ColumnEncryption.Data;
using Microsoft.ColumnEncryption.DataProviders;
using Microsoft.ColumnEncryption.Encoders;
using Microsoft.ColumnEncryption.EncryptionProviders;
using Microsoft.ColumnEncryption.Metadata;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;

namespace Microsoft.ColumnEncryption
{
    public class ColumnEncryptor
    {
        private readonly IConfigReader configReader;
        private readonly IConfigWriter configWriter;
        private readonly IKeyProtectorFactory keyProtectorFactory;
        private readonly IDataEncoder dataEncoder;
        private readonly IDataReader dataSource;
        private readonly IDataWriter dataSink;

        public ColumnEncryptor(
            IConfigReader configReader,
            IConfigWriter configWriter,
            IKeyProtectorFactory keyProtectorFactory,
            IDataEncoder dataEncoder,
            IDataReader dataSource,
            IDataWriter dataSink)
        {
            if (configReader == null) throw new ArgumentNullException(nameof(configReader));
            if (configWriter == null) throw new ArgumentNullException(nameof(configWriter));
            if (keyProtectorFactory == null) throw new ArgumentNullException(nameof(keyProtectorFactory));
            if (dataEncoder == null) throw new ArgumentNullException(nameof(dataEncoder));
            if (dataSource == null) throw new ArgumentNullException(nameof(dataSource));
            if (dataSink == null) throw new ArgumentNullException(nameof(dataSink));

            this.configReader = configReader;
            this.configWriter = configWriter;
            this.keyProtectorFactory = keyProtectorFactory;
            this.dataEncoder = dataEncoder;
            this.dataSource = dataSource;
            this.dataSink = dataSink;
        }

        // TODO: make these virtual methods
        public void Encrypt()
        {
            DataProtectionConfig config = this.configReader.Read();
            IReadOnlyDictionary<string, (ColumnKeyInfo keyInfo, ColumnKeyMaterial keyMaterial)> columnKeys =
                this.GetClearColumnKeys(config.ColumnMasterKeyInfo, config.ColumnKeyInfo);

            // Update config with the generated & encrypted column key
            UpdateConfigWithEncryptedColumnKeys(config, columnKeys);

            // Encrypt column data using the decrypted column key
            IEnumerable<ColumnData> columnData = this.dataSource.Read();
            List<ColumnData> encryptedColumnData = new List<ColumnData>();
            foreach (ColumnData col in columnData)
            {
                if (ColumnEncryptor.IsProtectedColumn(config.ColumnEncryptionInfo, col.Name, out ColumnEncryptionInfo encryptionInfo))
                {
                    IEnumerable<byte[]> dataBytes = this.dataEncoder.ToBytes(col.Data, col.Type);

                    IDataProtector dataProtector = new AlwaysEncryptedDataProtector(
                        columnKeys[encryptionInfo.ColumnKeyName].keyMaterial.ClearColumnKey,
                        encryptionInfo.EncryptionType,
                        encryptionInfo.Algorithm);

                    IEnumerable<byte[]> encryptedDataBytes = dataProtector.Encrypt(dataBytes);
                    IEnumerable<string> encryptedHexString = encryptedDataBytes.Select(d => Converter.ToHexString(d));
                    ColumnData encryptedCol = new ColumnData(col.Name, col.Type)
                    {
                        Data = new List<object>(encryptedHexString)
                    };

                    encryptedColumnData.Add(encryptedCol);
                }
                else
                {
                    encryptedColumnData.Add(col);
                }
            }

            this.dataSink.Write(encryptedColumnData);
        }

        public void Decrypt()
        {
            DataProtectionConfig config = this.configReader.Read();
            IReadOnlyDictionary<string, (ColumnKeyInfo keyInfo, ColumnKeyMaterial keyMaterial)> columnKeys =
                this.GetClearColumnKeys(config.ColumnMasterKeyInfo, config.ColumnKeyInfo);

            IEnumerable<ColumnData> columnData = this.dataSource.Read();
            List<ColumnData> decryptedColumnData = new List<ColumnData>();
            foreach (ColumnData col in columnData)
            {
                if (ColumnEncryptor.IsProtectedColumn(config.ColumnEncryptionInfo, col.Name, out ColumnEncryptionInfo encryptionInfo))
                {
                    IEnumerable<byte[]> dataBytes = col.Data.Select(d => Converter.FromHexString((string)d));

                    IDataProtector dataProtector = new AlwaysEncryptedDataProtector(
                        columnKeys[encryptionInfo.ColumnKeyName].keyMaterial.ClearColumnKey,
                        encryptionInfo.EncryptionType,
                        encryptionInfo.Algorithm);

                    IEnumerable<byte[]> decryptedBytes = dataProtector.Decrypt(dataBytes);
                    IEnumerable<object> decryptedData = this.dataEncoder.FromBytes(decryptedBytes, col.Type);

                    ColumnData encryptedCol = new ColumnData(col.Name, col.Type)
                    {
                        Data = new List<object>(decryptedData)
                    };

                    decryptedColumnData.Add(encryptedCol);
                }
                else
                {
                    decryptedColumnData.Add(col);
                }
            }

            this.dataSink.Write(decryptedColumnData);
        }

        /// <summary> Determines if a particular column needs to be protected </summary>
        /// <param name="columnEncryptionInfo"> List of column encryption key </param>
        /// <param name="name"> Column name </param>
        /// <param name="columnKeyName"> Column key name if column is to be protected </param>
        /// <returns> True if column is to be protected, false if not </returns>
        private static bool IsProtectedColumn(IEnumerable<ColumnEncryptionInfo> columnEncryptionInfo, string name, out ColumnEncryptionInfo encryptionInfo)
        {
            encryptionInfo = columnEncryptionInfo.FirstOrDefault(
                c => c.ColumnName.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return encryptionInfo != null;
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

        /// <summary> Gets clear column keys either by generating new one or by decrypting already encrypted keys </summary>
        /// <param name="columnMasterKeyInfoList"> List of master key info </param>
        /// <param name="columnKeyInfoList"> List of column key info </param>
        /// <returns> Map of column key name to corresponding column key info and clear column key </returns>
        private Dictionary<string, (ColumnKeyInfo keyInfo, ColumnKeyMaterial keyMaterial)> GetClearColumnKeys(
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
                IKeyProtector keyProtector = this.keyProtectorFactory.Get(masterKeyInfo);

                if (columnKeyInfo.EncryptedColumnKey == null)
                {
                    columnKeyMaterial.ClearColumnKey = ColumnEncryptor.GenerateColumnKey();
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

        private void UpdateConfigWithEncryptedColumnKeys(
            DataProtectionConfig config,
            IReadOnlyDictionary<string, (ColumnKeyInfo keyInfo, ColumnKeyMaterial keyMaterial)> columnKeys)
        {
            foreach (ColumnKeyInfo configKeyInfo in config.ColumnKeyInfo)
            {
                if (configKeyInfo.EncryptedColumnKey == null || configKeyInfo.EncryptedColumnKey.Length == 0)
                {
                    configKeyInfo.EncryptedColumnKey = columnKeys[configKeyInfo.Name].keyInfo.EncryptedColumnKey;
                }
            }

            this.configWriter.Write(config);
        }
    }
}
*/