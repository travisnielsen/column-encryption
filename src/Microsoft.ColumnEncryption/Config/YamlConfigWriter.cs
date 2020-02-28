using Microsoft.ColumnEncryption.Metadata;
using System;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.ColumnEncryption.Config
{
    /// <summary> Provides config data using a backing yaml file </summary>
    public class YamlConfigWriter : IConfigWriter
    {
        private readonly string configFilePath;
        private Stream configFile;
        bool configStream = false;

        /// <summary> Initializes a new instance of <see cref="YamlConfigWriter"/> class </summary>
        /// <param name="configFilePath"> Config file path to write to </param>
        public YamlConfigWriter(string configFilePath)
        {
            if (string.IsNullOrWhiteSpace(configFilePath)) throw new ArgumentNullException(nameof(configFilePath));

            this.configFilePath = configFilePath;
        }

        /// <summary> Initializes a new instance of <see cref="YamlConfigWriter"/> class  </summary>
        /// <param name="configFile"> Config file stream object</param>
        public YamlConfigWriter(Stream configFile)
        {
            this.configFile = configFile;
            configStream = true;
        }

        /// <summary> Writes updated config back to yaml file </summary>
        /// <param name="currentConfig"> Latest config data </param>
        public void Write(DataProtectionConfig currentConfig)
        {
            if (configStream)
            {
                using (StreamWriter streamWriter = new StreamWriter(configFile))
                {
                    ISerializer serializer = new SerializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
                    serializer.Serialize(streamWriter, currentConfig);
                }
            }
            else
            {
                using (StreamWriter streamWriter = new StreamWriter(this.configFilePath, false, Encoding.UTF8))
                {
                    ISerializer serializer = new SerializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
                    serializer.Serialize(streamWriter, currentConfig);
                }
            }
        }
    }
}