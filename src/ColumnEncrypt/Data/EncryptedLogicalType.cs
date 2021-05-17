using System;
using Avro;
using Avro.Util;

namespace ColumnEncrypt.Data
{
    public class EncryptedLogicalType : LogicalType
    {
        public static readonly string LogicalTypeName = "encrypted";

        public EncryptedLogicalType() : base(LogicalTypeName) { }

        public override object ConvertToBaseValue(object logicalValue, LogicalSchema schema)
        {
            throw new NotImplementedException();
        }

        public override object ConvertToLogicalValue(object baseValue, LogicalSchema schema)
        {
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
    }
}