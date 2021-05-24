using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Encryption.FileEncryption;

namespace ColumnEncrypt.Data
{
    /// <summary> Holds all information about a column and its data </summary>
    public class ColumnData : IColumn
    {
        private List<object> _dataList { get; set; }

        public string Name { get; set; }
        public int Index { get; set; }
        public Type DataType { get; set; }
        public Array Data
        {
            get
            {
                if (typeof(Int32) == DataType)
                {
                    return _dataList.OfType<int>().ToArray();
                }
                if (typeof(long) == DataType)
                {
                    return _dataList.OfType<long>().ToArray();
                }
                if (typeof(float) == DataType)
                {
                    return _dataList.OfType<float>().ToArray();
                }
                if (typeof(double) == DataType)
                {
                    return _dataList.OfType<double>().ToArray();
                }
                if (typeof(byte[]) == DataType)
                {
                    return _dataList.OfType<byte[]>().ToArray();
                }
                else
                {
                    return _dataList.OfType<string>().ToArray();
                }
            }
            set
            {
                Data = value;
            }
        }


        /// <summary> Initializes a new instance of <see cref="ColumnData"/> </summary>
        /// <param name="Name"> Column name </param>
        public ColumnData(string Name) : this(Name, typeof(string))
        {
        }

        /// <summary> Initializes a new instance of <see cref="ColumnData"/> </summary>
        /// <param name="name"> Column name </param>
        /// <param name="type"> Column data type </param>
        public ColumnData(string name, Type type)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (type == null) throw new ArgumentNullException(nameof(type));

            this.DataType = type;
            this.Name = name;
            _dataList = new List<object>();
        }
        
        public void AddColumnRecord(object o)
        {
            _dataList.Add(o);
        }

    }
}