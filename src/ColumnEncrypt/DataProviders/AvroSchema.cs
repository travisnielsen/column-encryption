using System.Collections.Generic;
using System.Text.Json.Serialization;
using Avro;

namespace ColumnEncrypt.DataProviders
{
    public class AvroSchema
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("namespace")]
        public string Namespace { get; set; }

        [JsonPropertyName("fields")]
        public List<Field> Fields { get; set; }

    }
}