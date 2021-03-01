using ColumnEncryption.Util.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ColumnEncryption.Util.Encoders
{
    public class DefaultEncoder : IDataEncoder
    {
        public IEnumerable<object> FromBytes(IEnumerable<byte[]> data, Type type)
        {
            if (type == typeof(string))
            {
                return data.Select(d => Encoding.ASCII.GetString(d)).ToList();
            }

            throw new NotImplementedException($"Encoding not implemented for type {nameof(type)}");
        }

        public IEnumerable<byte[]> ToBytes(IEnumerable<object> data, Type type)
        {
            if (type == typeof(string))
            {
                return data.Select(d => Encoding.ASCII.GetBytes((string)d)).ToList();
            }

            throw new NotImplementedException($"Encoding not implemented for type {nameof(type)}");
        }
    }
}
