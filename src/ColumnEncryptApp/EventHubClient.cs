using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace ColumnEncrypt.App
{
    public static class EventHubClient
    {  
        public static async Task SendEvent(string connectionString, string filePath)
        {
            byte[] eventBytes = File.ReadAllBytes(filePath);
            ReadOnlyMemory<byte> memoryBytes = new ReadOnlyMemory<byte>(eventBytes);
            EventData eventBody = new EventData(memoryBytes);

            var producer = new EventHubProducerClient(connectionString, "userdata");

            try
            {
                EventDataBatch eventBatch = await producer.CreateBatchAsync();
                Console.WriteLine("duh");
                if (!eventBatch.TryAdd(eventBody))
                    throw new Exception("error creating event body");

                await producer.SendAsync(eventBatch);
                Console.WriteLine("Batch has been published.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                await producer.DisposeAsync();
            }

        }

    }
}