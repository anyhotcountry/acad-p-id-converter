using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace BP.ConvertDwg
{
    public class Config
    {

        public string Source { get; set; }

        public string Destination { get; set; }

        public IList<Tag> Tags { get; set; }

        public static Config Load(string path)
        {
            try
            {
                var yml = File.ReadAllText(Path.Combine(path, "config.yaml"));
                var deserializer = new DeserializerBuilder().Build();
                var config = deserializer.Deserialize<Config>(yml);
                return config;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        public class Tag
        {
            public string Pattern { get; set; }

            public string Name { get; set; }
        }
    }
}
