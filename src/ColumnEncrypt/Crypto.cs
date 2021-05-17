using System;
using System.IO;
using Azure.Core;
using ColumnEncrypt.Config;
using ColumnEncrypt.DataProviders;
using ColumnEncrypt.Metadata;
using ColumnEncrypt.Util;
using Microsoft.Data.Encryption.AzureKeyVaultProvider;
using Microsoft.Data.Encryption.FileEncryption;

namespace ColumnEncrypt
{
    public static class Crypto
    {
        public static void FileTransform(IColumnarDataReader reader, IColumnarDataWriter writer)
        {
            ColumnarCryptographer cryptographer = new ColumnarCryptographer (reader, writer);

            try
            {
                cryptographer.Transform ();
                Console.WriteLine ($"File processed successfully. Verify output file contains encrypted data.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void FileTransform(FileData input, FileData output, DataProtectionConfig config, TokenCredential credential)
        {
            ColumnarCryptographer cryptographer;

            if (input.FileType == FileType.csv)
            {
                using var csvReader = new CSVDataReader(new StreamReader(input.FilePath), config, credential, input.IsEncrypted);
                
                if (output.FileType == FileType.csv)
                {
                    using var csvWriter = new CSVDataWriter (new StreamWriter(output.FilePath), config, credential, csvReader.Header, output.IsEncrypted);
                    cryptographer = new ColumnarCryptographer (csvReader, csvWriter);
                    cryptographer.Transform ();
                }
                else if (output.FileType == FileType.parquet)
                {
                    using var parquetWriter = new ParquetFileWriter (File.OpenWrite(output.FilePath), ColumnSettings.Load(config, null, new AzureKeyVaultKeyStoreProvider(credential), output.IsEncrypted));
                    cryptographer = new ColumnarCryptographer (csvReader, parquetWriter);
                    cryptographer.Transform ();
                }
                else if (output.FileType == FileType.avro)
                {
                    throw new NotImplementedException("Avro format not implemented");
                }
                else
                {
                    throw new ArgumentException("file extension is not recognized");
                }
            }
            else if (input.FileType == FileType.parquet)
            {
                var parquetReader = new ParquetFileReader(File.OpenRead(input.FilePath));

                if (output.FileType == FileType.csv)
                {
                    // TOOD: need to implement logic for parquet read to csv write
                    using var csvWriter = new CSVDataWriter (new StreamWriter(output.FilePath), config, credential, null, output.IsEncrypted);
                    cryptographer = new ColumnarCryptographer (parquetReader, csvWriter);
                    cryptographer.Transform ();
                }
                else if (output.FileType == FileType.parquet)
                {
                    using var parquetWriter = new ParquetFileWriter (File.OpenWrite(output.FilePath), ColumnSettings.Load(config, null, new AzureKeyVaultKeyStoreProvider(credential), output.IsEncrypted));
                    cryptographer = new ColumnarCryptographer (parquetReader, parquetWriter);
                    cryptographer.Transform ();
                }
                else if (output.FileType == FileType.avro)
                {
                    throw new NotImplementedException("Avro format not implemented");
                }
                else
                {
                    throw new ArgumentException("file extension is not recognized");
                }

            }
            else if (input.FileType == FileType.avro)
            {
                throw new NotImplementedException("Avro format not implemented");
            }
            else
            {
                throw new ArgumentException("file extension is not recognized");
            }
        }

        private static IColumnarDataReader GetReader(FileData input, DataProtectionConfig config, TokenCredential credential)
        {
            if (input.FileType == FileType.csv)
            {
                var csvReader = new CSVDataReader(new StreamReader(input.FilePath), config, credential, input.IsEncrypted);
                return csvReader;
            }
            else if (input.FileType == FileType.parquet)
            {
                using var parquetReader = new ParquetFileReader(File.OpenRead(input.FilePath));
                return parquetReader;
            }
            else if (input.FileType == FileType.avro)
            {
                throw new NotImplementedException("Avro format not implemented");
            }
            else
            {
                throw new ArgumentException("file extension is not recognized");
            }
        }

        private static IColumnarDataWriter GetWriter(FileData output, IColumnarDataReader reader, DataProtectionConfig config, TokenCredential credential)
        {

            if (output.FileType == FileType.csv)
            {
                var csvReader = (CSVDataReader) reader;
                var csvWriter = new CSVDataWriter (new StreamWriter(output.FilePath), config, credential, csvReader.Header, output.IsEncrypted);
                return csvWriter;
            }
            else if (output.FileType == FileType.parquet)
            {
                using var parquetWriter = new ParquetFileWriter (File.OpenWrite(output.FilePath), ColumnSettings.Load(config, null, new AzureKeyVaultKeyStoreProvider(credential), output.IsEncrypted));
                return parquetWriter;
            }
            else if (output.FileType == FileType.avro)
            {
                throw new NotImplementedException("Avro format not implemented");
            }
            else
            {
                throw new ArgumentException("file extension is not recognized");
            }
        }


    }
}