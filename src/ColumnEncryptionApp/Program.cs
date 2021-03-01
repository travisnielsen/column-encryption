using McMaster.Extensions.CommandLineUtils;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Configuration;

using ColumnEncryption.Util.Auth;
using ColumnEncryption.Util.Config;
using ColumnEncryption.Util.DataProviders;

using Azure.Core;
using Azure.Identity;

using Microsoft.Data.Encryption.AzureKeyVaultProvider;
using Microsoft.Data.Encryption.Cryptography;
using Microsoft.Data.Encryption.Cryptography.Serializers;
using Microsoft.Data.Encryption.FileEncryption;
using System.Collections.Generic;
using System.Linq;

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

        public const string AzureKeyVaultKeyPath = "https://trniel.vault.azure.net/keys/contoso-sensitive/729cb571a1c3494a9c86ae2613bdd017";

        // New Token Credential to authenticate to Azure interactively.
        public static readonly TokenCredential TokenCredential = new InteractiveBrowserCredential();

        // Azure Key Vault provider that allows client applications to access a key encryption key is stored in Microsoft Azure Key Vault.
        public static readonly EncryptionKeyStoreProvider azureKeyProvider = new AzureKeyVaultKeyStoreProvider (TokenCredential);

        // Represents the key encryption key that encrypts and decrypts the data encryption key
        public static readonly KeyEncryptionKey keyEncryptionKey = new KeyEncryptionKey ("KEK", AzureKeyVaultKeyPath, azureKeyProvider);

        // Represents the encryption key that encrypts and decrypts the data items
        public static readonly ProtectedDataEncryptionKey encryptionKey = new ProtectedDataEncryptionKey ("DEK", keyEncryptionKey);

        [Argument(0, Description = "The command to execute.  'encrypt' or 'decrypt'.")]
        [Required]
        public string Command { get; }

        /*
        [Option(Description = "The path to the input data.")]
        [Required]
        public string DataFilePath { get; }
        */

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
            // string outPath = OutputFilePath ?? (Path.GetFileNameWithoutExtension(DataFilePath) + "_output" + Path.GetExtension(DataFilePath));

            // open input and output file streams
            Stream inputFile = File.OpenRead (".\\resources\\userdata1.parquet");
            Stream outputFile = File.OpenWrite (".\\resources\\userdata1-out.parquet");

            // Create reader
            using ParquetFileReader reader = new ParquetFileReader (inputFile);

            // Copy source settings as target settings
            List<FileEncryptionSettings> writerSettings = reader.FileEncryptionSettings
                .Select (s => Copy (s))
                .ToList ();

            // Modify a few column settings
            writerSettings[0] = new FileEncryptionSettings<DateTimeOffset?> (encryptionKey, SqlSerializerFactory.Default.GetDefaultSerializer<DateTimeOffset?> ());
            writerSettings[3] = new FileEncryptionSettings<string> (encryptionKey, EncryptionType.Deterministic, new SqlVarCharSerializer (size: 255));
            writerSettings[10] = new FileEncryptionSettings<double?> (encryptionKey, StandardSerializerFactory.Default.GetDefaultSerializer<double?> ());

            // Create and pass the target settings to the writer
            using ParquetFileWriter writer = new ParquetFileWriter (outputFile, writerSettings);

            // Process the file
            ColumnarCryptographer cryptographer = new ColumnarCryptographer (reader, writer);
            cryptographer.Transform ();

            Console.WriteLine ($"Parquet File processed successfully. Verify output file contains encrypted data.");

            /*
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
            */

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

        public static FileEncryptionSettings Copy (FileEncryptionSettings encryptionSettings) {
            Type genericType = encryptionSettings.GetType ().GenericTypeArguments[0];
            Type settingsType = typeof (FileEncryptionSettings<>).MakeGenericType (genericType);
            return (FileEncryptionSettings) Activator.CreateInstance (
                settingsType,
                new object[] {
                    encryptionSettings.DataEncryptionKey,
                        encryptionSettings.EncryptionType,
                        encryptionSettings.GetSerializer ()
                }
            );
        }


    }
}


