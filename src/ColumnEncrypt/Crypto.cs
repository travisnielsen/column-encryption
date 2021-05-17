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
            ColumnarCryptographer cryptographer = new ColumnarCryptographer(reader, writer);

            try
            {
                cryptographer.Transform();
                Console.WriteLine($"File processed successfully. Verify output file contains encrypted data.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        public static void FileTransform(FileData input, FileData output, DataProtectionConfig config, TokenCredential credential)
        {
            ColumnarCryptographer cryptographer;

            // initialize the reader & writer so checks can be done later for value
            IDisposable reader = null;
            IDisposable writer = null;
            string[] header = null;

            // FileType is csv, parquet, or avro - no other choices
            //  so the only option to fail would be avro.  No need to throw ArgumentException
            //  since the FileType is a non-nullable Enum and it has to be one of the 3 values 
            if (input.FileType == FileType.avro || output.FileType == FileType.avro)
            {
                throw new NotImplementedException("Avro format not implemented");
            }

            // switch on the input type and create the reader & the writer if "CSV" else create the parquet writer further down
            switch (input.FileType)
            {
                case FileType.csv:

                    reader = new CSVDataReader(new StreamReader(input.FilePath), config, credential, input.IsEncrypted);

                    // set the unique header value if output is CSV
                    if (output.FileType == FileType.csv)
                    {
                        header = ((CSVDataReader)reader).Header;
                    }
                    break;


                case FileType.parquet:
                    // Just create the reader, leave the header null
                    reader = new ParquetFileReader(File.OpenRead(input.FilePath));
                    break;
            }

            // Create the writer object
            switch (output.FileType)
            {
                case FileType.csv:
                    // CSV writer has all the same parameters except for the header which is set above to null by default or then reset to the Header value of the reader
                    writer = new CSVDataWriter(new StreamWriter(output.FilePath), config, credential, header, output.IsEncrypted);
                    break;

                case FileType.parquet:
                    // parquet writer looks to be identical for either input type
                    writer = new ParquetFileWriter(File.OpenWrite(output.FilePath), ColumnSettings.Load(config, null, new AzureKeyVaultKeyStoreProvider(credential), output.IsEncrypted));
                    break;
            }

            // Got all the values, do the work
            using (reader)
            {
                using (writer)
                {
                    cryptographer = new ColumnarCryptographer((IColumnarDataReader)reader, (IColumnarDataWriter)writer);
                    cryptographer.Transform();
                }
            }
        }


        private static IColumnarDataReader GetReader(FileData input, DataProtectionConfig config, TokenCredential credential)
        {
            IColumnarDataReader reader = null;

            switch (input.FileType)
            {
                case FileType.csv:
                    reader = new CSVDataReader(new StreamReader(input.FilePath), config, credential, input.IsEncrypted);
                    break;

                case FileType.parquet:
                    reader = new ParquetFileReader(File.OpenRead(input.FilePath));
                    break;

                case FileType.avro:
                    throw new NotImplementedException("Avro format not implemented");
            }

            return reader;
        }


        private static IColumnarDataWriter GetWriter(FileData output, IColumnarDataReader reader, DataProtectionConfig config, TokenCredential credential)
        {
            IColumnarDataWriter writer = null;

            switch (output.FileType)
            {
                case FileType.csv:
                    writer = new CSVDataWriter(new StreamWriter(output.FilePath), config, credential, ((CSVDataReader)reader).Header, output.IsEncrypted);
                    break;

                case FileType.parquet:
                    writer = new ParquetFileWriter(File.OpenWrite(output.FilePath), ColumnSettings.Load(config, null, new AzureKeyVaultKeyStoreProvider(credential), output.IsEncrypted));
                    break;

                case FileType.avro:
                    throw new NotImplementedException("Avro format not implemented");
            }

            return writer;
        }
    }
}