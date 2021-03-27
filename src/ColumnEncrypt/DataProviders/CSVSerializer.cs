using Microsoft.Data.Encryption.Cryptography.Serializers;

namespace ColumnEncrypt.DataProviders
{
    public class CSVSerializer : Serializer<string>
    {
        public override string Identifier => "CSV";
        
        public override string Deserialize(byte[] bytes)
        {
            throw new System.NotImplementedException();
        }

        public override byte[] Serialize(string value)
        {
            throw new System.NotImplementedException();
        }
    }

}