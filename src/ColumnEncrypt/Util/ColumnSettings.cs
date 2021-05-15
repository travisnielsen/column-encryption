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
                    // KeyEncryptionKey defaultKek = new KeyEncryptionKey("default", "https://akv4h4jnwnsm6ehu.vault.azure.net/keys/mde-sensitive/38300751e540463c93eb1b503be8d61d", azureKeyProvider);
                    // byte[] dekBytes = Converter.FromHexString("0x01B8000002680074007400700073003A002F002F0061006B0076003400680034006A006E0077006E0073006D0036006500680075002E007600610075006C0074002E0061007A007500720065002E006E00650074002F006B006500790073002F006D00640065002D00730065006E007300690074006900760065002F0033003800330030003000370035003100650035003400300034003600330063003900330065006200310062003500300033006200650038006400360031006400C87141EB4381EAF1FEBA3A304561F61C372DEC2DA18CD640FD95D34D643DD3303A7043CB06655C6F14BCF8D22A60CCDA69EE29F63ACA0386D5D729BD121CE3FAED9644C68B9BA6C0E59AF250316F0D2A54F9A840349CDC7294097E3FE0CE573E3F72F8B70D33EE366B39ED33C06B6C49AFEA0253EF3827B6BDEA52A057F7D96DE8170848BFF2A651145FE9F5E01C9484C1FB8025D9623DC553E09314405AA6BEDC461F5E0758D47EFC98A3532406357161073EAB242B4D6B6EC4EB26BF2812D1AC5F6209863E717752BD1AE2FE89B72EB01C216A1E442B75425DA7DE1217495F7F6DD2BDAF319A4F2154B88A9DE0DD8B5A5F55568513935E0B64E4424413AD1AA7D1CA27CDB3B14BCE0D3B1D445A1BAB8FF1EC7283FD6D385E666FD81F62E9FA47B203C7215DB3BC4A0331D69031B74A755A8C6DFD3B8D7EF6FC99A398696C9C7F93A7A3513DEE9C0276E3155342AE2819D88DDE5019C928E60182D90AED38462361668CD642375C83E544C36A5C9DC30B0F019D3B2B5F9732F2DD0CA8AA3679160EBB64ED0F64D162E8E550AD6F608B7E1D095D1F7FBC9E4E51F46EA761A9BB3A7E5E9FD5CB9ED2CAAF55838AB4E2A4E4E0F72D33F249580F0588D14BCDA14C39D95A468648096D84F8EBBA850B7FDC6D302BFC5E5B469E1F3219DE6AB24096F6B326B9F053B3AE60042132EDDADBE00FB1BC270B9DF64678339C0A40436D6F56E012CCA538E0643A97B09FFAC060CFEF092EF7C07C7D4951AD7766DD3272C8085AD77B111150B2A53F46C202DCB02ACA80E6CEBCAB6365FCDB3BCF53E29C02B848B20C5011A9FDF0832550F4FC5B2BFA93CEEEFD4748452FCE8401063FE7A58358C6B49F3D4DD4214C4AC7355F436E0362C4430DE8C1D487901841EED72E66BDAF0502FDD55CCF696CBD0CB8E835AFB2AB7413BA54D54FA597F26EF483293E9432416EB09B9051708D4B8BE59B9BA175C4EE94F7F712898168923A7142854407819FF61723A81E1A2CA250E3AE92E534D1F340A7DFF14D8C8B51012E146F820200647AE4C1D9ECC4A2B20B1BABB6632F8B3CA15853B29B744EE4DC3D7205346B3AA7C8323168A1B7BED126B63E8ED76F9122928B9D6829D27370B2C88260F7100661BC53303DB10C43FE1223CC83A5FA8C0AA92E419ED3EF87CFCC0845FD7A2C9C9B673FDE70691CDB34B5B3102C96211479E13B5E0819C0E1478200C62D2DE62488D87DC2BE156E909DB840F9D161A529B342B404E7A60735A05ECB77E96B599F9FF9DC0CF076CC644B560E1001C57C904073F72098190510C10DCE154291F647F3E7398016EDFE6134C8217C9171B0061A3174B3B1F8A8F80B8A95643D51BFB683498EBE583D90AC0394599B88539F04B00EB98210F4C08FA10B60A0AE1FAB733E625EB1206AF7247E577D667BD8F0D0840B2AEF45C5F95B3C1D8787D1D3");
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
