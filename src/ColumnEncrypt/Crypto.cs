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
        public static void FileTransform(FileData input, FileData output, DataProtectionConfig config, TokenCredential credential, string[] columns)
        {
            ColumnarCryptographer cryptographer;
            IDisposable reader = null;
            IDisposable writer = null;
            Dictionary<int, string> transformColumnIndexes = null;
            IList<FileEncryptionSettings> readerEncryptionSettings = null;

            switch (input.FileType)
            {
                case FileType.csv:
                    reader = new CSVFileReader(new StreamReader(input.FilePath));
                    string[] header = ((CSVFileReader)reader).Header;
                    transformColumnIndexes = GetColumnIndexes(header, columns);
                    var encryptionSettings = ColumnSettings.GetEncryptionSettings(config, header, new AzureKeyVaultKeyStoreProvider(credential), input.IsEncrypted);
                    ((CSVFileReader)reader).FileEncryptionSettings = encryptionSettings;
                    break;

                case FileType.parquet:
                    Dictionary<string, EncryptionKeyStoreProvider> parquetEncryptionKeyStoreProviders = new Dictionary<string, EncryptionKeyStoreProvider>();
                    parquetEncryptionKeyStoreProviders.Add("AZURE_KEY_VAULT", new AzureKeyVaultKeyStoreProvider(credential));
                    reader = new ParquetFileReader(File.OpenRead(input.FilePath), parquetEncryptionKeyStoreProviders);
                    transformColumnIndexes = GetColumnIndexes(((ParquetFileReader)reader), columns);
                    readerEncryptionSettings = ((ParquetFileReader)reader).FileEncryptionSettings;
                    break;

                case FileType.avro:
                    Dictionary<string, EncryptionKeyStoreProvider> avroEncryptionKeyStoreProviders = new Dictionary<string, EncryptionKeyStoreProvider>();
                    avroEncryptionKeyStoreProviders.Add("AZURE_KEY_VAULT", new AzureKeyVaultKeyStoreProvider(credential));
                    reader = new AvroFileReader(File.OpenRead(input.FilePath), avroEncryptionKeyStoreProviders);
                    transformColumnIndexes = GetColumnIndexes(((AvroFileReader)reader).FieldNames, columns);
                    readerEncryptionSettings = ((AvroFileReader)reader).FileEncryptionSettings;
                    break;
            }

            switch (output.FileType)
            {
                case FileType.csv:
                    IList<FileEncryptionSettings> csvWriterSettings = ColumnSettings.GetWriterSettings(((IColumnarDataReader)reader).FileEncryptionSettings, transformColumnIndexes, new AzureKeyVaultKeyStoreProvider(credential), output.IsEncrypted);
                    writer = new CSVFileWriter(new StreamWriter(output.FilePath), csvWriterSettings);
                    break;

                case FileType.parquet:
                    IList<FileEncryptionSettings> parquetWriterSettings = ColumnSettings.GetWriterSettings(((IColumnarDataReader)reader).FileEncryptionSettings, transformColumnIndexes, new AzureKeyVaultKeyStoreProvider(credential), output.IsEncrypted);
                    writer = new ParquetFileWriter(File.OpenWrite(output.FilePath), parquetWriterSettings);
                    break;
                
                case FileType.avro:
                    IList<FileEncryptionSettings> avroWriterSettings = ColumnSettings.GetWriterSettings(((IColumnarDataReader)reader).FileEncryptionSettings, transformColumnIndexes, new AzureKeyVaultKeyStoreProvider(credential), output.IsEncrypted);
                    writer = new AvroFileWriter(new StreamWriter(output.FilePath), avroWriterSettings , output.Schema);
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

        /// <summary>Returns a dictionary of column indexes and names. </summary>
        /// <param name="reader"> An instance of <c>ParquetFileReader</c> </param>
        /// <param name="transformColumns"> Specfic column names targeted for crypto operations </param>
        /// <returns> A dictionary of matching names and the header index </returns>
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

        /// <summary>Returns a dictionary of column indexes and names. </summary>
        /// <param name="fields"> A sorted list of column names in a text file </param>
        /// <param name="transformColumns"> Specfic column names targeted for crypto operations </param>
        /// <returns> A dictionary of matching names and the header index </returns>
        private static Dictionary<int, string> GetColumnIndexes(string[] fields, string[] transformcColumns)
        {
            Dictionary<int, string> columnIndexes = new Dictionary<int, string>();

            for (int i = 0; i < fields.Length; i++)
            {
                if (transformcColumns.Contains(fields[i].ToLower()))
                {
                    columnIndexes.Add(i, fields[i]);
                }
            }

            return columnIndexes;
        }

    }
}