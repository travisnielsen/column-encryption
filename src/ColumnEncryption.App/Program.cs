using McMaster.Extensions.CommandLineUtils;
using Microsoft.ColumnEncryption;
using Microsoft.ColumnEncryption.Config;
using Microsoft.ColumnEncryption.DataProviders;
using Microsoft.ColumnEncryption.Encoders;
using Microsoft.ColumnEncryption.EncryptionProviders;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Configuration;
using Microsoft.ColumnEncryption.Auth;

namespace ColumnEncryption.App
{
    public enum OutputTargets
    {
        LOCAL = 0,   // Local file system
        ADLS = 1    // Azure Data Lake Store
    }

    public class Program
    {
        private const string APP_SETTING_ADLS_ACCOUNT_NAME = "adlsAccountName";
        private const string APP_SETTING_ADLS_CONTAINER_PATH = "adlsContainerPath";
        private const string APP_SETTING_TENANT_ID = "tenantId";
        private const string APP_SETTING_APPLICATION_ID = "appId";
        private const string APP_SETTING_APP_REDIRECT_URI = "appRedirectUri";
        private const string APP_SETTING_APPLICATION_NAME = "appName";
        private const string APP_SETTING_APPLICATION_VERSION = "appVersion";
        private const string APP_SETTING_AUTH_ENDPOINT = "authEndpointUrl";

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

            using (CSVDataReader csvDataReader = new CSVDataReader(new StreamReader(DataFilePath)))
            using (CSVDataWriter csvDataWriter = new CSVDataWriter(new StreamWriter(outPath)))
            {
                var columnEncryptor = new ColumnEncryptor(
                    new YamlConfigReader(MetadataFilePath),
                    new YamlConfigWriter(MetadataFilePath),
                    new KeyProtectorFactory(
                        new Microsoft.ColumnEncryption.Common.Settings()
                        {
                            AppId = ConfigurationManager.AppSettings[APP_SETTING_APPLICATION_ID],
                            AppName = ConfigurationManager.AppSettings[APP_SETTING_APPLICATION_NAME],
                            AppVersion = ConfigurationManager.AppSettings[APP_SETTING_APPLICATION_VERSION],
                            CloudEndpointBaseUrl = ConfigurationManager.AppSettings[APP_SETTING_AUTH_ENDPOINT],
                            ApplicationUri = ConfigurationManager.AppSettings[APP_SETTING_APP_REDIRECT_URI]

                        },
                        new AuthProvider(
                            ConfigurationManager.AppSettings[APP_SETTING_APPLICATION_ID],
                            ConfigurationManager.AppSettings[APP_SETTING_APP_REDIRECT_URI])),
                    new DefaultEncoder(),
                    csvDataReader,
                    csvDataWriter);

                switch (Command.ToLower())
                {
                    case "encrypt":
                        Console.WriteLine("Getting things ready to encrypt and store to csv...");
                        columnEncryptor.Encrypt();
                        Console.WriteLine("Done encrypting.");
                        break;

                    case "decrypt":
                        Console.WriteLine("Getting things ready to decrypt and store to csv...");
                        columnEncryptor.Decrypt();
                        Console.WriteLine("Done decrypting.");
                        break;

                    default:
                        Console.WriteLine("Not a valid command. Try 'encrypt' or 'decrypt' as a command.");
                        break;
                }
            }

            /*
            if (OutputTarget == OutputTargets.ADLS)
            {
                DataLakeGen2Client adlsClient = new DataLakeGen2Client(
                    ConfigurationManager.AppSettings[APP_SETTING_ADLS_ACCOUNT_NAME],
                    ConfigurationManager.AppSettings[APP_SETTING_ADLS_CONTAINER_PATH],
                    ConfigurationManager.AppSettings[APP_SETTING_TENANT_ID],
                    ConfigurationManager.AppSettings[APP_SETTING_APPLICATION_ID],
                    ConfigurationManager.AppSettings[APP_SETTING_APP_REDIRECT_URI]);

                // Upload the file to ADLS
                Console.WriteLine("Uploading file to ADLS");
                using (FileStream fs = new FileStream(outPath, FileMode.Open))
                {
                    adlsClient.CreateFileAsync(outPath, fs).GetAwaiter().GetResult();
                }
            }
            */
        }
    }
}


