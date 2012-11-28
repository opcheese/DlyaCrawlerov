using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HierarchicalClassification
{
    public class Post
    {
        private List<string> _tags;
        private List<WordIndex> _words;
        private string _content;

        public ObjectId Id { get; set; }
        [BsonElement("content")]
        public string Content
        {
            get
            {
                if (_content == null)
                    _content = string.Empty;
                return _content.Trim();
            }
            set
            {
                _content = value;
            }
        }
        [BsonElement("theme")]
        public string Theme { get; set; }
        [BsonElement("author")]
        public string Author { get; set; }
        [BsonElement("dt")]
        public DateTime DT { get; set; }
        [BsonElement("words")]
        public List<WordIndex> Words
        {
            get
            {
                if (_words == null)
                    _words = new List<WordIndex>();
                return _words.Where(w => !string.IsNullOrWhiteSpace(w.Word)).Where(w => w.Word.Length > 3).Where(w => w.Word[0] != '&').ToList();
            }
            set
            {
                _words = value;
            }
        }
        [BsonElement("tags")]
        public List<string> Tags
        {
            get
            {
                if (_tags == null)
                    _tags = new List<string>();
                return _tags;
            }
            set
            {
                _tags = value;
            }
        }
    }
}
