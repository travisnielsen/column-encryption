using System;
using System.Collections.Generic;
using System.Linq;
using ColumnEncrypt.Metadata;
using Microsoft.Data.Encryption.Cryptography;
using Microsoft.Data.Encryption.Cryptography.Serializers;
using Microsoft.Data.Encryption.FileEncryption;

namespace ColumnEncrypt.Util
{
    public static class ColumnSettings
    {
        private static bool encryption = false;

        public static IList<FileEncryptionSettings> Load(DataProtectionConfig config, string[] header, EncryptionKeyStoreProvider azureKeyProvider, bool encryption)
        {
            List<FileEncryptionSettings> encryptionSettings = new List<FileEncryptionSettings>();

            for (int i = 0; i < header.Length; i++)
            {
                ColumnEncryptionInfo encryptionInfo = config.ColumnEncryptionInfo.Where(x => x.ColumnName == header[i]).FirstOrDefault();

                if (encryptionInfo != null) // this column has encryption info
                {
                    string dekName = encryptionInfo.ColumnKeyName;

                    ColumnKeyInfo dekInfo = config.ColumnKeyInfo.First(x => x.Name == encryptionInfo.ColumnKeyName);
                    byte[] dekBytes = Converter.FromHexString(dekInfo.EncryptedColumnKey);

                    ColumnMasterKeyInfo kekInfo = config.ColumnMasterKeyInfo.First(x => x.Name == dekInfo.ColumnMasterKeyName);
                    KeyEncryptionKey kek = new KeyEncryptionKey(kekInfo.Name, kekInfo.KeyPath, azureKeyProvider);

                    EncryptionType encryptionType = EncryptionType.Plaintext;
                    
                    if (encryption)
                    {
                        if (encryptionInfo.EncryptionType.ToLower() == "randomized")
                            encryptionType = EncryptionType.Randomized;
                        else if (encryptionInfo.EncryptionType.ToLower() == "deterministic")
                            encryptionType = EncryptionType.Deterministic;
                        else
                            encryptionType = EncryptionType.Plaintext;
                    }
                    
                    FileEncryptionSettings<string> encryptionSetting = new FileEncryptionSettings<string>(new ProtectedDataEncryptionKey(dekName, kek, dekBytes), encryptionType, new SqlVarCharSerializer (size: 255));
                    encryptionSettings.Add(encryptionSetting);
                }
                else
                {
                    FileEncryptionSettings<string> encryptionSetting = new FileEncryptionSettings<string>(null, EncryptionType.Plaintext, new SqlVarCharSerializer (size: 255));
                    encryptionSettings.Add(encryptionSetting);
                }
            }

            return encryptionSettings;
        }

        private static FileEncryptionSettings Copy (FileEncryptionSettings encryptionSettings) {
            Type genericType = encryptionSettings.GetType ().GenericTypeArguments[0];
            Type settingsType = typeof (FileEncryptionSettings<>).MakeGenericType (genericType);
            return (FileEncryptionSettings) Activator.CreateInstance (
                settingsType,
                new object[] {
                    encryptionSettings.DataEncryptionKey,
                        encryptionSettings.EncryptionType,
                        encryptionSettings.GetSerializer ()
                }
            );
        }


    }
}
