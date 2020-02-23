using Microsoft.ColumnEncryption.Common;
using Microsoft.ColumnEncryption.Config;
using Microsoft.ColumnEncryption.Data;
using Microsoft.ColumnEncryption.Encoders;
using Microsoft.ColumnEncryption.EncryptionProviders;
using Microsoft.ColumnEncryption.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ColumnEncryption
{
    public class StreamingColumnEncryptor
    {
        private readonly IConfigReader configReader;
        private readonly IConfigWriter configWriter;
        private readonly IKeyProtectorFactory keyProtectorFactory;
        private readonly IDataEncoder dataEncoder;
        private IReadOnlyDictionary<string, (ColumnKeyInfo keyInfo, ColumnKeyMaterial keyMaterial)> columnKeys;
        private DataProtectionConfig config;

        public StreamingColumnEncryptor(
            IConfigReader configReader,
            IConfigWriter configWriter,
            IKeyProtectorFactory keyProtectorFactory,
            IDataEncoder dataEncoder)
        {
            if (configReader == null) throw new ArgumentNullException(nameof(configReader));
            if (configWriter == null) throw new ArgumentNullException(nameof(configWriter));
            if (keyProtectorFactory == null) throw new ArgumentNullException(nameof(keyProtectorFactory));
            if (dataEncoder == null) throw new ArgumentNullException(nameof(dataEncoder));

            this.configReader = configReader;
            this.configWriter = configWriter;
            this.keyProtectorFactory = keyProtectorFactory;
            this.dataEncoder = dataEncoder;
        }

        public void Initialize()
        {
            this.config = this.configReader.Read();
            KeyInfoValidator.ValidateKeyInfo(this.config.ColumnKeyInfo, this.config.ColumnMasterKeyInfo);

            this.columnKeys = ColumnKeyManager.GetClearColumnKeys(this.keyProtectorFactory, this.config.ColumnMasterKeyInfo, this.config.ColumnKeyInfo);

            // Update config with the generated & encrypted column key
            UpdateConfigWithEncryptedColumnKeys(config, columnKeys);
        }

        // TODO: make these virtual methods
        public IEnumerable<ColumnData> Encrypt(IEnumerable<ColumnData> columnData)
        {
            if (this.config == null || this.columnKeys == null)
                throw new Exception("Initialization not complete yet. Initialize must be called before this operation.");

            List<ColumnData> encryptedColumnData = new List<ColumnData>();
            foreach (ColumnData col in columnData)
            {
                if (this.IsProtectedColumn(col.Name, out ColumnEncryptionInfo encryptionInfo))
                {
                    IEnumerable<byte[]> dataBytes = this.dataEncoder.ToBytes(col.Data, col.Type);

                    IDataProtector dataProtector = new AlwaysEncryptedDataProtector(
                        this.columnKeys[encryptionInfo.ColumnKeyName].keyMaterial.ClearColumnKey,
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

            return encryptedColumnData;
        }

        public IEnumerable<ColumnData> Decrypt(IEnumerable<ColumnData> columnData)
        {
            if (this.config == null || this.columnKeys == null)
                throw new Exception("Initialization not complete yet. Initialize must be called before this operation.");

            List<ColumnData> decryptedColumnData = new List<ColumnData>();
            foreach (ColumnData col in columnData)
            {
                if (this.IsProtectedColumn(col.Name, out ColumnEncryptionInfo encryptionInfo))
                {
                    IEnumerable<byte[]> dataBytes = col.Data.Select(d => Converter.FromHexString((string)d));

                    IDataProtector dataProtector = new AlwaysEncryptedDataProtector(
                        this.columnKeys[encryptionInfo.ColumnKeyName].keyMaterial.ClearColumnKey,
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

            return decryptedColumnData;
        }

        /// <summary> Determines if a particular column needs to be protected </summary>
        /// <param name="columnEncryptionInfo"> List of column encryption key </param>
        /// <param name="name"> Column name </param>
        /// <param name="columnKeyName"> Column key name if column is to be protected </param>
        /// <returns> True if column is to be protected, false if not </returns>
        private bool IsProtectedColumn(string name, out ColumnEncryptionInfo encryptionInfo)
        {
            encryptionInfo = this.config.ColumnEncryptionInfo.FirstOrDefault(
                c => c.ColumnName.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return encryptionInfo != null;
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