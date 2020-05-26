using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace NetTaskInterface
{
    public class Configuration
    {
        Dictionary<string, Config> configs = new Dictionary<string, Config>();
        string path;

        public string EntryPoint { get; private set; }

        internal Configuration() : this(System.IO.Path.Combine(new System.IO.FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).Directory.FullName, "main.xml"))
        {

        }
        public Configuration(string path)
        {
            this.path = path;
            if (!System.IO.File.Exists(path))
                return;
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(path);
            EntryPoint = xmldoc.DocumentElement.Attributes["entrypoint"].Value;
            foreach (XmlNode node in xmldoc.DocumentElement.ChildNodes)
            {
                Dictionary<string, string> dic = new Dictionary<string, string>();
                foreach (XmlNode kv in node.SelectNodes("add"))
                {
                    dic.Add(kv.Attributes["key"].Value, kv.Attributes["value"].Value);
                }
                if (!configs.ContainsKey(node.Name))
                    configs.Add(node.Name, new Config(node.Name, dic, this));
            }
        }

        public Config GetConfig(Type type)
        {
            if (!configs.ContainsKey(type.FullName))
                return new Config(type.FullName, null, this);
            return configs[type.FullName];
        }

        internal void SaveConfig(Config config, IEnumerable<KeyValuePair<string, string>> values)
        {
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(path);
            foreach (var kv in values)
            {
                var node = xmldoc.DocumentElement.SelectSingleNode(string.Format("{0}/add[@key='{1}']", config.name, kv.Key));
                if (node != null)
                {
                    node.Attributes["value"].Value = kv.Value;
                    config.dic[kv.Key] = kv.Value;
                }
            }
            xmldoc.Save(path);
        }

        public class Config
        {
            private Configuration configuration;
            internal Dictionary<string, string> dic;
            internal string name;
            internal Config(string name, Dictionary<string, string> dic, Configuration configuration)
            {
                this.configuration = configuration;
                this.name = name;
                this.dic = dic;
                if (this.dic == null)
                    this.dic = new Dictionary<string, string>();
            }

            public string this[string key]
            {
                get
                {
                    if (dic.ContainsKey(key))
                        return dic[key];
                    return null;
                }
            }

            public DateTime? GetDateTimeValue(string key)
            {
                var value = this[key];
                DateTime result;
                if (!string.IsNullOrEmpty(value) && DateTime.TryParse(value, out result))
                    return result;
                return null;
            }

            public int? GetIntValue(string key)
            {
                var value = this[key];
                int result;
                if (!string.IsNullOrEmpty(value) && int.TryParse(value, out result))
                    return result;
                return null;
            }

            public float? GetFloatValue(string key)
            {
                var value = this[key];
                float result;
                if (!string.IsNullOrEmpty(value) && float.TryParse(value, out result))
                    return result;
                return null;
            }

            public double? GetDoubleValue(string key)
            {
                var value = this[key];
                double result;
                if (!string.IsNullOrEmpty(value) && double.TryParse(value, out result))
                    return result;
                return null;
            }

            public IEnumerable<KeyValuePair<string, string>> Values
            {
                get
                {
                    return dic.Select(p => p);
                }
            }

            public void Save(IEnumerable<KeyValuePair<string, string>> values)
            {
                configuration.SaveConfig(this, values);
            }
        }

    }
}
