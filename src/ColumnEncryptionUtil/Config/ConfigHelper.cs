using System;
using System.Collections.Generic;
using System.Linq;
using ColumnEncryption.Util.Common;
using ColumnEncryption.Util.Metadata;
using Microsoft.Data.Encryption.Cryptography;
using Microsoft.Data.Encryption.Cryptography.Serializers;
using Microsoft.Data.Encryption.FileEncryption;

namespace ColumnEncryption.Util.Config
{
    public static class ConfigHelper
    {
        public static FileEncryptionSettings Copy (FileEncryptionSettings encryptionSettings) {
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