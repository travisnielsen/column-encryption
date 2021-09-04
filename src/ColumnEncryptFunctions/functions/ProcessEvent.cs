using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using Azure.Core;
using Avro.File;
using Avro.Generic;
using System.Text.Json;
using System.IO;
using Avro.Util;
using ColumnEncrypt.DataProviders;
using ColumnEncrypt.Metadata;
using System.Collections.Generic;

namespace ColumnEncryption.Functions
{
    public static class ProcessEvent
    {
        private static LogicalTypeFactory _logicalTypeFactory = LogicalTypeFactory.Instance;

        [Function("ProcessEvent")]
        public static void Run([EventHubTrigger("userdata", Connection = "EVENTHUB_CONNECTION")] byte[][] input, FunctionContext context)
        {
            var logger = context.GetLogger("ProcessEvent");
            var stream = new MemoryStream(input[0]);

            TokenCredential credential = new DefaultAzureCredential();
            EncryptedLogicalTypeStream encryptedLogicalType = new EncryptedLogicalTypeStream(credential);
            _logicalTypeFactory.Register(encryptedLogicalType);

            using (var reader = DataFileReader<GenericRecord>.OpenReader(stream, false))
            {
                var metadataKeys = reader.GetMetaKeys();
                
                if (metadataKeys.Contains("columnKeyInfo")) {
                    List<ColumnKeyInfo> columnKeyInfo = JsonSerializer.Deserialize<List<ColumnKeyInfo>>(reader.GetMetaString("columnKeyInfo"));
                    List<ColumnMasterKeyInfo> columnMasterKeyInfo = JsonSerializer.Deserialize<List<ColumnMasterKeyInfo>>(reader.GetMetaString("columnMasterKeyInfo"));
                    DataProtectionConfig config = new DataProtectionConfig { ColumnKeyInfo = columnKeyInfo, ColumnMasterKeyInfo = columnMasterKeyInfo };
                    encryptedLogicalType.EncryptionConfig = config;     // update the logical type with encryption config. This can't be provided during object creation because its embedded in the file
                }

                while (reader.HasNext())
                {
                    GenericRecord record = reader.Next();
                    object fieldData = null;
                    record.TryGetValue("SSN", out fieldData);
                    logger.LogInformation($"SSN: {fieldData}");
                }
            }

        }
    }
}
