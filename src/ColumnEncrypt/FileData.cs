using System.IO;

namespace ColumnEncrypt
{
    public class FileData
    {
        public FileType FileType
        { 
            get
            {
                string extension = FilePath.Substring(FilePath.LastIndexOf('.') + 1);

                switch(extension.ToLower())
                {
                    case ("csv"):
                        return FileType.csv;
                    case ("parquet"):
                        return FileType.parquet;
                    case ("avro"):
                        return FileType.avro;
                    default:
                        throw new System.Exception("invalid file type. Must be .avro, .csv, or .parquet");
                }
            }
        }
        
        public bool IsEncrypted { get; }

        public string FilePath { get; }

        public string Schema { get; }

        public FileData(string filePath, bool isEncrypted, string schema)
        {
            IsEncrypted = isEncrypted;
            FilePath = filePath;
            Schema = schema;
        }
    }
}