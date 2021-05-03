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
            IColumnarDataReader reader = null;
            IColumnarDataWriter writer = null;

            if (input.FileType == FileType.csv)
            {
                CSVDataReader csvReader = new CSVDataReader(new StreamReader(input.FilePath), config, credential, input.IsEncrypted);
                reader = csvReader;
            }
            else if (input.FileType == FileType.parquet)
            {
                using ParquetFileReader parquetReader = new ParquetFileReader(File.OpenRead(input.FilePath));
                reader = parquetReader;
            }
            else if (input.FileType == FileType.avro)
            {
                throw new NotImplementedException("Avro format not implemented");
            }
            else
            {
                throw new ArgumentException("file extension is not recognized");
            }

            if (output.FileType == FileType.csv)
            {
                var csvReader = (CSVDataReader) reader;

                // TODO: Need to look a proper use of using statement for this case. Disposal happens too soon when set with: using CSVDataWriter csvWriter = new CSVDataWriter ...
                CSVDataWriter csvWriter = new CSVDataWriter (new StreamWriter(output.FilePath), config, credential, csvReader.Header, output.IsEncrypted);
                writer = csvWriter;
            }
            if (output.FileType == FileType.parquet)
            {
                using ParquetFileWriter parquetWriter = new ParquetFileWriter (File.OpenWrite(output.FilePath), ColumnSettings.Load(config, null, new AzureKeyVaultKeyStoreProvider(credential), output.IsEncrypted));
                writer = parquetWriter;
            }
            if (output.FileType == FileType.avro)
            {
                throw new NotImplementedException("Avro format not implemented");
            }

            ColumnarCryptographer cryptographer = new ColumnarCryptographer (reader, writer);

            try
            {
                cryptographer.Transform ();
                Console.WriteLine ($"File processed successfully. Verify output file contains encrypted data.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw (e);
            }
        }

    }
}