using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ColumnEncryption;
using Microsoft.ColumnEncryption.Auth;
using Microsoft.ColumnEncryption.Config;
using Microsoft.ColumnEncryption.DataProviders;
using Microsoft.ColumnEncryption.Encoders;
using Microsoft.ColumnEncryption.EncryptionProviders;
using Microsoft.Extensions.Logging;

namespace ColumnEncryption.Functions
{
    public static class ProtectCsv
    {
        [FunctionName("ProtectCsv")]
        public static void Run(
            [BlobTrigger("csv/{csvName}", Connection = "AzureWebJobsStorage")]Stream csvFile, string csvName,
            [Blob("config/TestConfig.yaml", FileAccess.Read)] Stream configFile,
            [Blob("output/{csvName}", FileAccess.Write)] Stream outputFile,
            ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{csvName} \n Size: {csvFile.Length} Bytes");

            CSVDataReader csvDataReader = new CSVDataReader(new StreamReader(csvFile));


            using (CSVDataWriter csvDataWriter = new CSVDataWriter(new StreamWriter(outputFile)))
            {
                var columnEncryptor = new ColumnEncryptor(
                    new YamlConfigReader(configFile),
                    new YamlConfigWriter(configFile),
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
            }
        }
    }
}
