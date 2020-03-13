using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetTaskServer.DB
{
    public class KV
    {
        public KV()
        {
        }

        public KV(string k, string val)
        {
            Key = k;
            Value = val;
        }

        [BsonId]
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
