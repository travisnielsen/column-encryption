using ColumnEncryption.Util.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColumnEncryption.Util.Encoders
{
    public interface IDataEncoder
    {
        IEnumerable<object> FromBytes(IEnumerable<byte[]> data, Type type);

        IEnumerable<byte[]> ToBytes(IEnumerable<object> data, Type type);
    }
}
