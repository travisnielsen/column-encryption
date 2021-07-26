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
        public static IList<FileEncryptionSettings> GetEncryptionSettings(DataProtectionConfig config, string[] columnList, EncryptionKeyStoreProvider azureKeyProvider, bool encryption)
        {
            List<FileEncryptionSettings> encryptionSettings = new List<FileEncryptionSettings>();

            // Set a default key from config. This is required for columns that are not part of encryption
            ColumnKeyInfo defaultDekInfo = config.ColumnKeyInfo.FirstOrDefault();
            byte[] defaultDekBytes = Converter.FromHexString(defaultDekInfo.EncryptedColumnKey);

            ColumnMasterKeyInfo defaultKekInfo = config.ColumnMasterKeyInfo.First(x => x.Name == defaultDekInfo.ColumnMasterKeyName);
            KeyEncryptionKey defaultKek = new KeyEncryptionKey(defaultKekInfo.Name, defaultKekInfo.KeyPath, azureKeyProvider);

            for (int i = 0; i < columnList.Length; i++)
            {
                ColumnEncryptionInfo encryptionInfo = config.ColumnEncryptionInfo.Where(x => x.ColumnName == columnList[i]).FirstOrDefault();

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
                    // This column is not part of any encryption - use default key - cannot currently pass in null or invalid keys, even if they will never be used
                    FileEncryptionSettings<string> encryptionSetting = new FileEncryptionSettings<string>(new ProtectedDataEncryptionKey("none", defaultKek, defaultDekBytes), EncryptionType.Plaintext, new SqlVarCharSerializer (size: 255));
                    encryptionSettings.Add(encryptionSetting);
                }
            }

            return encryptionSettings;
        }

        public static FileEncryptionSettings GetColumnEncryptionSettings<T>(ProtectedDataEncryptionKey encryptionKey, EncryptionType encryptionType)
        {
            if (typeof(T) == typeof(Int32))
                return new FileEncryptionSettings<int>(encryptionKey, encryptionType, new Int32Serializer());
            if (typeof(T) == typeof(float))
                return new FileEncryptionSettings<double>(encryptionKey, encryptionType, new DoubleSerializer());
            if (typeof(T) == typeof(double))
                return new FileEncryptionSettings<double>(encryptionKey, encryptionType, new DoubleSerializer());
            if (typeof(T) == typeof(byte[]))
                return new FileEncryptionSettings<byte[]>(encryptionKey, encryptionType, new ByteArraySerializer());
            else
                return new FileEncryptionSettings<string>(encryptionKey, encryptionType, new SqlVarCharSerializer(size: 255));
        }

        public static IList<FileEncryptionSettings> GetWriterSettings(IList<FileEncryptionSettings> readerSettings, Dictionary<int, string> columnIndexes, EncryptionKeyStoreProvider azureKeyProvider, bool encryption)
        {
            List<FileEncryptionSettings> writerSettings = readerSettings.Select(s => Copy(s)).ToList();

            // TODO: Review this code and look at options to base writer settings on config metadata when encryptiong (deterministic vs. randomized) and/or setting keys. Also serializers based on reader data type

            foreach (var item in columnIndexes)
            {
                var readerEncryptionSetting = readerSettings[item.Key];

                if (encryption)
                {
                    writerSettings[item.Key] = new FileEncryptionSettings<string>(readerEncryptionSetting.DataEncryptionKey, EncryptionType.Randomized, new SqlVarCharSerializer (size: 255));
                }
                else
                {
                    writerSettings[item.Key] = new FileEncryptionSettings<string>(readerEncryptionSetting.DataEncryptionKey, EncryptionType.Plaintext, new SqlVarCharSerializer (size: 255));
                }
            }

            return writerSettings;
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
