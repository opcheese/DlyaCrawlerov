using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HierarchicalClassification
{
    public partial class Form1 : Form
    {
        private readonly MongoServer _server;
        private readonly MongoDatabase _databaseCrawler;
        private readonly MongoCollection<Post> _posts;

        public Form1()
        {
            InitializeComponent();

            //var urlTo = MongoUrl.Create("mongodb://mongodb-endorphin.cloudapp.net:27017");
            var urlTo = MongoUrl.Create("mongodb://localhost:27017");
            var settingsTo = urlTo.ToServerSettings();
            _server = MongoServer.Create(settingsTo);
            _databaseCrawler = _server.GetDatabase("SyutkinCrawler");
            _posts = _databaseCrawler.GetCollection<Post>("Posts");
        }

        private void GetNode(List<Post> posts, List<string> StopWords, TreeNode parentNode)
        {
            var avgWordCounts = posts
                                .SelectMany(p => p.Words.Where(w => !StopWords.Contains(w.Word)).Select(w => new { Word = w.Word, AvgCount = (double)w.Count / (p.Content.Length + 300) }))
                                .GroupBy(w => w.Word)
                                .Select(w => new { Word = w.Key, AvgCount = w.Sum(a => a.AvgCount) / posts.Count })
                                .ToList();            
            var nextWords = avgWordCounts
                        .Select(avg => new
                            {
                                Word = avg.Word,
                                Avg = avg.AvgCount,
                                Dispersion = posts
                                                        .SelectMany(p => p.Words.Where(w => w.Word == avg.Word)
                                                        .Select(w => new { Word = w.Word, AvgCount = (double)w.Count / (p.Content.Length + 300) }))
                                                        //.Sum(w => Math.Sign(w.AvgCount - avg.AvgCount))
                                                        .Sum(w => Math.Abs(w.AvgCount - avg.AvgCount))  //это убирает стоп слова
                            })
                        .ToList();

            var nextWord = nextWords.OrderBy(w => -w.Dispersion).FirstOrDefault();              //sum(abs(...))
            //var nextWord = nextWords.OrderBy(w => -Math.Abs(w.Dispersion)).FirstOrDefault();  //sign(...)
            if (nextWord == null)
                return;

            List<string> newStopWords = StopWords.Clone().ToList();
            newStopWords.Add(nextWord.Word);

            List<Post> inPosts = posts.Where(p => p.Words.Select(w => new { Word = w.Word, AvgCount = (double)w.Count / (p.Content.Length + 300) }).SingleOrDefault(w => w.Word == nextWord.Word).With(w => w.AvgCount) > nextWord.Avg).ToList();
            List<Post> outPosts = posts.Where(p => p.Words.Select(w => new { Word = w.Word, AvgCount = (double)w.Count / (p.Content.Length + 300) }).SingleOrDefault(w => w.Word == nextWord.Word).With(w => w.AvgCount) <= nextWord.Avg).ToList();

            TreeNode addNode = parentNode.Nodes.Add(string.Format("add {0}", nextWord.Word));
            TreeNode remNode = parentNode.Nodes.Add(string.Format("rem {0}", nextWord.Word));
            addNode.BackColor = Color.LightGreen;
            remNode.BackColor = Color.LightPink;
            addNode.Tag = inPosts;
            remNode.Tag = outPosts;

            if (inPosts.Count > 1)
                GetNode(inPosts, newStopWords, addNode);
            if (outPosts.Count > 1)
                GetNode(outPosts, newStopWords, remNode);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            List<Post> posts = _posts
                                    .FindAll()
                                    .OrderBy(post => -post.Content.Length)
                                    //.SetLimit(20)
                                    .ToList()
                                    .GetRange(0, 20)
                                    .ToList();
            TreeNode node = treeViewClassification.Nodes.Add("Main");
            GetNode(posts, new List<string>(), node);
            node.Tag = posts;
            node.ExpandAll();
        }

        private void treeViewClassification_AfterSelect(object sender, TreeViewEventArgs e)
        {
            listBoxWords.Items.Clear();
            List<Post> posts = (List<Post>)e.Node.Tag;
            listBoxPosts.DataSource = posts.Select(p => p.Content).ToList();

            TreeNode currentNode = e.Node;
            while (currentNode.Level > 0)
            {
                if (currentNode.Text.Substring(0, 3) == "add")
                {
                    string tag = currentNode.Text.Substring(4);
                    listBoxWords.Items.Insert(0, tag);
                }
                currentNode = currentNode.Parent;
            }

        }

        private void listBoxPosts_SelectedIndexChanged(object sender, EventArgs e)
        {
            richTextBox1.Text = string.Empty;
            if (listBoxPosts.Items.Count > 0)
                richTextBox1.Text = listBoxPosts.SelectedItem.ToString();
        }
    }
}
