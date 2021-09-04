using System;
using System.IO;
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using ColumnEncrypt.Config;
using ColumnEncrypt.Metadata;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace ColumnEncrypt.App
{
    public class Program
    {
        private static readonly TokenCredential tokenCredential = new InteractiveBrowserCredential();

        [Argument(0, Description = "The command to execute: 'encrypt', 'decrypt', or 'stream'.")]
        [Required]
        public string Command { get; }

        [Option(Description = "The path to the yaml file specifying column policy metadata and key info.")]
        public string MetadataFilePath { get; }

        [Option(Description = "The path to the input data.", LongName = "input", ShortName = "i")]
        [Required]
        public string InputFilePath { get; }

        [Option(Description = "The path to the output data.", LongName = "output", ShortName = "o")]
        public string OutputFilePath { get; }

        [Option(Description = "The path to the schema file. Used for Avro", LongName = "schema", ShortName = "s")]
        public string SchemaFilePath { get; }     

        [Option(Description = "comma-separated list of columns to apply crypto operations against", LongName = "columns", ShortName = "c")]
        public string Columns { get; }

        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        private async void OnExecute()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();

            string inputExtension = Path.GetExtension(InputFilePath);
            string outPath = OutputFilePath ?? (Path.GetFileNameWithoutExtension(InputFilePath) + "_output" + inputExtension);

            DataProtectionConfig protectionConfig = null;

            if (MetadataFilePath != null)
            {
                YamlConfigReader configFile = new YamlConfigReader(MetadataFilePath);
                protectionConfig = configFile.Read();
            }

            string[] columns = new string[0];

            if (! String.IsNullOrEmpty(Columns))
            {
                columns = Columns.Split(",");
            }
            
            FileData sourceFile = null;
            FileData targetFile = null;

            string avroSchema = null;
            if (SchemaFilePath != null)
            {
                try
                {
                    avroSchema = System.IO.File.ReadAllText(SchemaFilePath);
                }
                catch (IOException ex)
                {
                    Console.WriteLine(ex.Message);
                    Environment.Exit(1);
                }
            }

            switch (Command.ToLower())
            {
                case "encrypt":
                    sourceFile = new FileData(InputFilePath, false, avroSchema);
                    targetFile = new FileData(outPath, true, avroSchema);
                    ColumnEncrypt.Crypto.FileTransform(sourceFile, targetFile, protectionConfig, tokenCredential, columns);
                    break; 
                case "decrypt":
                    sourceFile = new FileData(InputFilePath, true, avroSchema);
                    targetFile = new FileData(outPath, false, avroSchema);
                    ColumnEncrypt.Crypto.FileTransform(sourceFile, targetFile, protectionConfig, tokenCredential, columns);
                    break;
                case "stream":
                    string connectionString = config["eventHubConnectionString"];
                    if (String.IsNullOrEmpty(connectionString)) throw new Exception("Missing 'eventHubConnectionString' in appsettings.json");
                    await EventHubClient.SendEvent(connectionString, InputFilePath);
                    break;
                default:
                    Console.WriteLine("Not a valid command. Try 'encrypt', 'decrypt', or 'stream' as a command.");
                    break;
            }

        }
    }
}