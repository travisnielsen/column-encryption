using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ColumnEncryption;
using Microsoft.ColumnEncryption.Auth;
using Microsoft.ColumnEncryption.Config;
using Microsoft.ColumnEncryption.DataProviders;
using Microsoft.ColumnEncryption.Encoders;
using Microsoft.ColumnEncryption.EncryptionProviders;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ColumnEncryption.Functions
{
    public static class ProtectCsv
    {
        [FunctionName("ProtectCsv")]
        public static async Task Run(
            [BlobTrigger("csvinput/{csvName}", Connection = "AzureWebJobsStorage")]Stream csvFile, string csvName,
            [Blob("config/ClinicConfig.yaml", FileAccess.ReadWrite)] CloudBlockBlob configFile,
            [Blob("csvprotected/{csvName}", FileAccess.Write)] Stream outputFile,
            ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{csvName} \n Size: {csvFile.Length} Bytes");

            CSVDataReader csvDataReader = new CSVDataReader(new StreamReader(csvFile));

            Stream configInputStream = await configFile.OpenReadAsync();
            Stream configOutputStream = await configFile.OpenWriteAsync();

            using (CSVDataWriter csvDataWriter = new CSVDataWriter(new StreamWriter(outputFile)))
            {
                var columnEncryptor = new ColumnEncryptor(
                    new YamlConfigReader(configInputStream),
                    new YamlConfigWriter(configOutputStream),
                    new KeyProtectorFactory(
                        new Microsoft.ColumnEncryption.Common.Settings()
                        {
                            AppId = "",
                            AppName = "",
                            AppVersion = "",
                            CloudEndpointBaseUrl = "",
                            ApplicationUri = ""

                        },
                        new AuthProvider("", "")),
                    new DefaultEncoder(),
                    csvDataReader,
                    csvDataWriter);

                log.LogInformation("Getting things ready to encrypt and store to csv...");
                columnEncryptor.Encrypt();
                log.LogInformation("Done encrypting.");

                // Cleanup
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(System.Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference("csvinput");
                var blockBlob = container.GetBlockBlobReference($"{csvName}");
                bool deleted = await blockBlob.DeleteIfExistsAsync();
                if (deleted) { log.LogInformation($"Deleted source file: {csvName}"); }
            }
        }
    }
}
