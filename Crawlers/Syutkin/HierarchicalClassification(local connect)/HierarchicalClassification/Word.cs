using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HierarchicalClassification
{
    public class WordIndex
    {
        [BsonElement("word")]
        public string Word { get; set; }
        [BsonElement("count")]
        public int Count { get; set; }
    }
}
