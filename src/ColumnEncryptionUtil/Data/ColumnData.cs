using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Encryption.FileEncryption;

namespace ColumnEncryption.Util.Data
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
                return _dataList.ToArray();
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

            this.Name = name;
            this.DataType = type;
            // this.Data = new Array;
            _dataList = new List<object>();
        }
        
        public void AddColumnRecord(object o)
        {
            _dataList.Add(o);
        }

    }
}