using McMaster.Extensions.CommandLineUtils;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

using ColumnEncrypt.Config;
using ColumnEncrypt.Metadata;
using ColumnEncrypt.DataProviders;

using Azure.Core;
using Azure.Identity;

using Microsoft.Data.Encryption.FileEncryption;

namespace ColumnEncrypt.App
{
    public enum OutputTargets
    {
        LOCAL = 0,   // Local file system
        ADLS = 1    // Azure Data Lake Store
    }

    public class Program
    {
        // New Token Credential to authenticate to Azure interactively.
        public static readonly TokenCredential TokenCredential = new InteractiveBrowserCredential();

        [Argument(0, Description = "The command to execute.  'encrypt' or 'decrypt'.")]
        [Required]
        public string Command { get; }

        [Option(Description = "The path to the yaml file specifying column policy metadata and key info.")]
        [Required]
        public string MetadataFilePath { get; }

        [Option(Description = "The path to the input data.")]
        [Required]
        public string DataFilePath { get; }

        [Option(Description = "The output target, such as local file system or ADLS.  Default is 'LOCAL'.",
                LongName = "output-target",
                ShortName = "t")]
        public OutputTargets OutputTarget { get; }

        [Option(Description = "The path to the output data.",
                LongName = "output-file-path",
                ShortName = "o")]
        public string OutputFilePath { get; }

        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        private void OnExecute()
        {
            string outPath = OutputFilePath ?? (Path.GetFileNameWithoutExtension(DataFilePath) + "_output" + Path.GetExtension(DataFilePath));

            // load configuration file
            // YamlConfigReader configFile = new YamlConfigReader(".\\resources\\config.yaml");
            YamlConfigReader configFile = new YamlConfigReader(MetadataFilePath);
            DataProtectionConfig protectionConfig = configFile.Read();

            bool sourceIsEncrypted = false;
            bool targetIsEncrypted = true;
            string outputFileName = "";

            switch (Command.ToLower())
            {
                case "encrypt":
                    sourceIsEncrypted = false;
                    targetIsEncrypted = true;
                    outputFileName = DataFilePath.Split('.')[0] + "-encrypted." + DataFilePath.Split('.')[1];
                    break; 
                case "decrypt":
                    sourceIsEncrypted = true;
                    targetIsEncrypted = false;
                    outputFileName = DataFilePath.Split('.')[0] + "-decrypted." + DataFilePath.Split('.')[1];
                    break;
                default:
                    Console.WriteLine("Not a valid command. Try 'encrypt' or 'decrypt' as a command.");
                    break;
            }


            // For encryption operations, we're going to remove output settings

            // open input and output file streams
            // Stream inputFile = File.OpenRead (".\\resources\\userdata.parquet");
            // Stream outputFile = File.OpenWrite (".\\resources\\userdata.parquet");
            Stream outputFile = File.OpenWrite (outputFileName);

            // Create reader
            // using ParquetFileReader reader = new ParquetFileReader (inputFile);
            CSVDataReader reader = new CSVDataReader(new StreamReader(DataFilePath), protectionConfig, TokenCredential, sourceIsEncrypted);
            
            // Copy source settings as target settings
            /*
            List<FileEncryptionSettings> writerSettings = reader.FileEncryptionSettings
                .Select (s => Copy (s))
                .ToList ();
            */

            // Create and pass the target settings to the writer
            // using ParquetFileWriter writer = new ParquetFileWriter (outputFile, writerSettings);
            using CSVDataWriter writer = new CSVDataWriter (new StreamWriter(outputFile), protectionConfig, TokenCredential, reader.Header, targetIsEncrypted);

            // Process the file
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

    }
}


