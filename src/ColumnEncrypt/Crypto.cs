using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Azure.Core;
using ColumnEncrypt.Config;
using ColumnEncrypt.DataProviders;
using ColumnEncrypt.Metadata;
using ColumnEncrypt.Util;
using Microsoft.Data.Encryption.AzureKeyVaultProvider;
using Microsoft.Data.Encryption.Cryptography;
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


        public static void FileTransform(FileData input, FileData output, DataProtectionConfig config, TokenCredential credential, string[] columns)
        {
            ColumnarCryptographer cryptographer;

            // initialize the reader & writer so checks can be done later for value
            IDisposable reader = null;
            IDisposable writer = null;
            // string[] header = null;
            Dictionary<int, string> transformColumnIndexes = null;
            IList<FileEncryptionSettings> readerEncryptionSettings = null;

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
                    reader = new CSVDataReader(new StreamReader(input.FilePath));
                    string[] header = ((CSVDataReader)reader).Header;
                    transformColumnIndexes = GetColumnIndexes(header, columns);
                    var encryptionSettings = ColumnSettings.GetEncryptionSettings(config, header, new AzureKeyVaultKeyStoreProvider(credential), input.IsEncrypted);
                    ((CSVDataReader)reader).FileEncryptionSettings = encryptionSettings;
                    break;

                case FileType.parquet:
                    Dictionary<string, EncryptionKeyStoreProvider> encryptionKeyStoreProviders = new Dictionary<string, EncryptionKeyStoreProvider>();
                    encryptionKeyStoreProviders.Add("AZURE_KEY_VAULT", new AzureKeyVaultKeyStoreProvider(credential));
                    reader = new ParquetFileReader(File.OpenRead(input.FilePath), encryptionKeyStoreProviders);
                    transformColumnIndexes = GetColumnIndexes(((ParquetFileReader)reader), columns);
                    readerEncryptionSettings = ((ParquetFileReader)reader).FileEncryptionSettings;
                    break;
            }

            // Create the writer object
            switch (output.FileType)
            {
                case FileType.csv:
                    var writerSettings = ColumnSettings.GetWriterSettings(((IColumnarDataReader)reader).FileEncryptionSettings, transformColumnIndexes, new AzureKeyVaultKeyStoreProvider(credential), output.IsEncrypted);
                    writer = new CSVDataWriter(new StreamWriter(output.FilePath), writerSettings);
                    break;

                case FileType.parquet:
                    writer = new ParquetFileWriter(File.OpenWrite(output.FilePath), ColumnSettings.GetWriterSettings(((IColumnarDataReader)reader).FileEncryptionSettings, transformColumnIndexes, new AzureKeyVaultKeyStoreProvider(credential), output.IsEncrypted));
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

        /*
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
                    writer = new ParquetFileWriter(File.OpenWrite(output.FilePath), ColumnSettings.GetEncryptionSettings(config, null, new AzureKeyVaultKeyStoreProvider(credential), output.IsEncrypted));
                    break;

                case FileType.avro:
                    throw new NotImplementedException("Avro format not implemented");
            }

            return writer;
        }
        */

        private static Dictionary<int, string> GetColumnIndexes(ParquetFileReader reader, string[] transformColumns)
        {
            Dictionary<int, string> columnIndexes = new Dictionary<int, string>();

            var parquet = reader.Read();
            foreach (var item in parquet)
            {
                foreach (var column in item)
                {
                    if (transformColumns.Contains(column.Name.ToLower()))
                    {
                        columnIndexes.Add(column.Index, column.Name);
                    }
                }
            }

            return columnIndexes;
        }

        private static Dictionary<int, string> GetColumnIndexes(string[] header, string[] transformcColumns)
        {
            Dictionary<int, string> columnIndexes = new Dictionary<int, string>();

            for (int i = 0; i < header.Length; i++)
            {
                if (transformcColumns.Contains(header[i].ToLower()))
                {
                    columnIndexes.Add(i, header[i]);
                }
            }

            return columnIndexes;
        }

    }
}