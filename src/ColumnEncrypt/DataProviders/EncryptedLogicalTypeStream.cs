using System;
using Avro;
using Avro.Util;
using Azure.Core;
using Microsoft.Data.Encryption.AzureKeyVaultProvider;
using Microsoft.Data.Encryption.Cryptography;
using Microsoft.Data.Encryption.Cryptography.Serializers;
using ColumnEncrypt.Metadata;
using ColumnEncrypt.Util;
using System.Collections.Generic;
using System.Linq;

namespace ColumnEncrypt.DataProviders
{
    public class EncryptedLogicalTypeStream : LogicalType
    {
        public static readonly string LogicalTypeName = "encrypted";
        public DataProtectionConfig EncryptionConfig { get; set; }
        private static TokenCredential _credential;
        private static EncryptionKeyStoreProvider _azureKeyProvider;
        private List<ProtectedDataEncryptionKey> _dataEncryptionKeys;

        /// <summary>
        /// Support serialization / deserialization of Avro files when streaming Avro data fields
        /// </summary>
        public EncryptedLogicalTypeStream(TokenCredential credential) : base(LogicalTypeName)
        {
            _credential = credential;
            _azureKeyProvider = new AzureKeyVaultKeyStoreProvider(_credential);
        }

        public override object ConvertToBaseValue(object logicalValue, LogicalSchema schema)
        {
            // This is called when serializing / writing data
            // if logicalValue is encrypted, it will be byte[] return it
            return logicalValue;

            // if logiclaValue is string, need to convert it to a byte array
            // BUT: if you are serializing a field in plaintext, you should just change the schema and not use the logicaltype
        }

        public override object ConvertToLogicalValue(object baseValue, LogicalSchema schema)
        {
            if (schema.LogicalTypeName.ToLower() == "encrypted")
            {
                if (_dataEncryptionKeys == null)
                    _dataEncryptionKeys = SetDataEncryptionKeys();

                // TODO: Support all Avro types
                string dekName = schema.GetProperty("columnKeyName").Replace("\"", "").Replace("\\", "");
                string encryptionTypeInfo = schema.GetProperty("encryptionType").Replace("\"", "").Replace("\\", "");
                EncryptionType encryptionType = (encryptionTypeInfo.ToLower() == "randomized") ? EncryptionType.Randomized : EncryptionType.Deterministic;
                ProtectedDataEncryptionKey key = _dataEncryptionKeys.Where(x => x.Name == dekName).FirstOrDefault();
                var encryptionSettings = new EncryptionSettings<string>(key, encryptionType, new SqlVarCharSerializer (size: 255) );
                string decryptedValue = ((byte[])baseValue).Decrypt<string>(encryptionSettings);
                return decryptedValue;
            }
            else
                throw new NotImplementedException();
        }

        public override Type GetCSharpType(bool nullible)
        {
            throw new NotImplementedException();
        }

        public override bool IsInstanceOfLogicalType(object logicalValue)
        {
            throw new NotImplementedException();
        }

        public override void ValidateSchema(LogicalSchema schema)
        {
            if(Schema.Type.Bytes != schema.BaseSchema.Tag)
                throw new AvroTypeException("'encrypt' can only be used with an underlying byte[] type");
        }

        private List<ProtectedDataEncryptionKey> SetDataEncryptionKeys()
        {
            List<KeyEncryptionKey> keks = new List<KeyEncryptionKey>();
            List<ProtectedDataEncryptionKey> deks = new List<ProtectedDataEncryptionKey>();

            foreach(var kekInfo in EncryptionConfig.ColumnMasterKeyInfo )
            {
                KeyEncryptionKey kek = new KeyEncryptionKey(kekInfo.Name, kekInfo.KeyPath, _azureKeyProvider);
                keks.Add(kek);
            }

            foreach (var columnKey in EncryptionConfig.ColumnKeyInfo)
            {
                KeyEncryptionKey key = keks.Where(x => x.Name == columnKey.ColumnMasterKeyName).FirstOrDefault();
                byte[] dekBytes = Util.Converter.FromHexString(columnKey.EncryptedColumnKey);
                ProtectedDataEncryptionKey dek = new ProtectedDataEncryptionKey(columnKey.Name, key, dekBytes);
                deks.Add(dek);
            }

            return deks;
        }
    }

    /*
    public class CustomSerializer : Serializer<string>
    {
        private const int AgeSize = sizeof (int);
        private const int StringLengthSize = sizeof (int);
        private const int BytesPerCharacter = 2;
        private const int FirstNameIndex = AgeSize + StringLengthSize;

        public override string Identifier => "ColumnEncryptionString";

        public override string Deserialize(byte[] bytes)
        {

            string value =  .GetString(bytes);
        }

        public override byte[] Serialize (Person value) {
            byte[] ageBytes = BitConverter.GetBytes (value.Age);
            byte[] firstNameLengthBytes = BitConverter.GetBytes (value.FirstName.Length);
            byte[] firstNameBytes = Unicode.GetBytes (value.FirstName);
            byte[] lastNameLengthBytes = BitConverter.GetBytes (value.LastName.Length);
            byte[] lastNameBytes = Unicode.GetBytes (value.LastName);

            return ageBytes
                .Concat (firstNameLengthBytes)
                .Concat (firstNameBytes)
                .Concat (lastNameLengthBytes)
                .Concat (lastNameBytes)
                .ToArray ();
        }
    }
    */
}