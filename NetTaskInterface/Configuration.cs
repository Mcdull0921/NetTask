using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace NetTaskInterface
{
    public class Configuration
    {
        Dictionary<string, string> dic = new Dictionary<string, string>();

        public string Path { get; private set; }

        public string EntryPoint { get; private set; }

        public Configuration() : this(System.IO.Path.Combine(new System.IO.FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).Directory.FullName, "main.xml"))
        {

        }
        public Configuration(string path)
        {
            Path = path;
            if (!System.IO.File.Exists(path))
                return;
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(path);
            EntryPoint = xmldoc.DocumentElement.Attributes["entrypoint"].Value;
            foreach (XmlNode node in xmldoc.DocumentElement.SelectNodes("add"))
            {
                dic.Add(node.Attributes["key"].Value, node.Attributes["value"].Value);
            }
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
    }
}
