using ColumnEncrypt.Metadata;
using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ColumnEncrypt.Config
{
    /// <summary> Provides config data using a backing yaml file </summary>
    public class YamlConfigReader : IConfigReader
    {
        private readonly string configFile;
        private DataProtectionConfig yamlConfig;
        bool configExists = false;

        /// <summary> Initializes a new instance of <see cref="YamlConfigReader"/> class  </summary>
        /// <param name="configFilePath"> Config file path to be read from </param>
        public YamlConfigReader(string configFilePath)
        {
            if (string.IsNullOrWhiteSpace(configFilePath)) throw new ArgumentNullException(nameof(configFilePath));
            this.configFile = configFilePath;
        }

        /// <summary> Initializes a new instance of <see cref="YamlConfigReader"/> class  </summary>
        /// <param name="configFile"> Config file stream object</param>
        public YamlConfigReader(Stream configFile)
        {
            using (StreamReader reader = new StreamReader(configFile))
            {
                IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
                yamlConfig = deserializer.Deserialize<DataProtectionConfig>(reader);
                configExists = true;

                // TODO: Double-check on method to ensure this is disposed
                configFile.Close();
            }
        }

        /// <summary> Reads config data from yaml file </summary>
        /// <param name="reader"> Reader to read config from </param>
        /// <returns> Retrieved config </returns>
        public DataProtectionConfig Read()
        {
            if (configExists)
            {
                return yamlConfig;
            }
            else
            {
                using (StreamReader reader = File.OpenText(this.configFile))
                {
                    IDeserializer deserializer = new DeserializerBuilder()
                        .WithNamingConvention(PascalCaseNamingConvention.Instance)
                        .Build();

                    return deserializer.Deserialize<DataProtectionConfig>(reader);
                }
            }

        }
    }
}