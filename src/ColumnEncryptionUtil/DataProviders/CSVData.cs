using System.Collections.Generic;
using System.Linq;
using ColumnEncryption.Util.Common;
using ColumnEncryption.Util.Metadata;
using Microsoft.Data.Encryption.Cryptography;
using Microsoft.Data.Encryption.Cryptography.Serializers;
using Microsoft.Data.Encryption.FileEncryption;

namespace ColumnEncryption.Util.DataProviders
{
    public class CSVData
    {
        protected string[] header;
        protected EncryptionKeyStoreProvider azureKeyProvider;

        protected KeyEncryptionKey defaultKEK;
        protected IList<FileEncryptionSettings> encryptionSettings;

        protected IList<FileEncryptionSettings> LoadFileEncryptionSettings(DataProtectionConfig config, bool encrypted)
        {
            List<FileEncryptionSettings> encryptionSettings = new List<FileEncryptionSettings>();

            for (int i = 0; i < header.Length; i++)
            {
                ColumnEncryptionInfo encryptionInfo = config.ColumnEncryptionInfo.Where(x => x.ColumnName == header[i]).FirstOrDefault();

                if (encryptionInfo != null)
                {
                    string dekName = encryptionInfo.ColumnKeyName;

                    ColumnKeyInfo dekInfo = config.ColumnKeyInfo.First(x => x.Name == encryptionInfo.ColumnKeyName);
                    byte[] dekBytes = Converter.FromHexString(dekInfo.EncryptedColumnKey);

                    ColumnMasterKeyInfo kekInfo = config.ColumnMasterKeyInfo.First(x => x.Name == dekInfo.ColumnMasterKeyName);
                    KeyEncryptionKey kek = new KeyEncryptionKey(kekInfo.Name, kekInfo.KeyPath, azureKeyProvider);

                    EncryptionType encryptionType = EncryptionType.Plaintext;
                    if (encrypted)
                    {
                        if (encryptionInfo.EncryptionType.ToLower() == "randomized")
                            encryptionType = EncryptionType.Randomized;
                        else if (encryptionInfo.EncryptionType.ToLower() == "deterministic")
                            encryptionType = EncryptionType.Deterministic;
                        else
                            encryptionType = EncryptionType.Plaintext;
                    }

                    var encryptionSetting = new FileEncryptionSettings<string>(new ProtectedDataEncryptionKey(dekName, kek, dekBytes), encryptionType, new SqlVarCharSerializer (size: 255));
                    encryptionSettings.Add(encryptionSetting);
                }
                else
                {
                    if (defaultKEK == null)
                    {
                        ColumnMasterKeyInfo kekInfo = config.ColumnMasterKeyInfo.First();
                        KeyEncryptionKey kek = new KeyEncryptionKey(kekInfo.Name, kekInfo.KeyPath, azureKeyProvider);
                        defaultKEK = kek;
                    }
                    
                    var encryptionSetting = new FileEncryptionSettings<string>(new ProtectedDataEncryptionKey("none", defaultKEK) , EncryptionType.Plaintext, new SqlVarCharSerializer (size: 255));
                    encryptionSettings.Add(encryptionSetting);
                }
            }

            return encryptionSettings;
        }


    }
}