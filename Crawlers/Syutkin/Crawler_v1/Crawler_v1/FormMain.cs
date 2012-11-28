using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Linq;

namespace Crawler_v1
{
    public partial class FormMain : Form
    {
        private readonly MongoServer _server;
        private readonly MongoDatabase _databaseCrawler;
        private readonly MongoCollection _pages;
        private readonly MongoCollection _posts; 

        public FormMain()
        {
            InitializeComponent();
            //var urlTo = MongoUrl.Create("mongodb://mongodb-endorphin.cloudapp.net:27017");
            var urlTo = MongoUrl.Create("mongodb://localhost:27017");
            var settingsTo = urlTo.ToServerSettings();
            _server = MongoServer.Create(settingsTo);
            _databaseCrawler = _server.GetDatabase("SyutkinCrawler");
            _pages = _databaseCrawler.GetCollection("Pages");
            _posts = _databaseCrawler.GetCollection("Posts");
        }

        private void buttonDownload_Click(object sender, EventArgs e)
        {
            _pages.RemoveAll();
            _posts.RemoveAll();

            var stemmer = new Stemming();
            var i = 1;
            while (true)
            {
                try
                {
                    System.Threading.Thread.Sleep(100);
                    var urlBase = "http://ita2010.psu.ru:81";
                    var url = string.Format("{0}/?page={1}", urlBase, i++);
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.UserAgent = "MyApplication";
                    var response = (HttpWebResponse)request.GetResponse();
                    var dataStream = response.GetResponseStream();
                    var reader = new StreamReader(dataStream);
                    var responseFromServer = reader.ReadToEnd();
                    reader.Close();
                    dataStream.Close();
                    response.Close();

                    var doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(responseFromServer);
                    var allPosts = doc.DocumentNode.SelectNodes("//*[@id=\"blog-posts-content\"]/div");
                    for (int j = 1; j < allPosts.Count; j++)
                    {
                        System.Threading.Thread.Sleep(100);
                        var divAdress = string.Format("//*[@id=\"blog-posts-content\"]/div[{0}]/", j);

                        var link = doc.DocumentNode.SelectNodes(divAdress + "h2/a")[0].Attributes["href"].Value;
                        link = urlBase + link;
                        var requestContent = (HttpWebRequest)WebRequest.Create(link);
                        requestContent.UserAgent = "MyApplication";
                        var responseContent = (HttpWebResponse)requestContent.GetResponse();
                        var dataStreamContent = responseContent.GetResponseStream();
                        var readerContent = new StreamReader(dataStreamContent);
                        var responseFromServerContent = readerContent.ReadToEnd();
                        readerContent.Close();
                        dataStreamContent.Close();
                        responseContent.Close();
                        
                        var docContent = new HtmlAgilityPack.HtmlDocument();
                        docContent.LoadHtml(responseFromServerContent);
                        var post = new BsonDocument();
                        var text = docContent.DocumentNode.SelectNodes("//*[@id=\"workarea-content\"]/div[1]/div/div[2]")[0].InnerText;
                        
                        post["content"] = text;
                        var allWords = text.ToLower().Split(new char[]
                                            {
                                                ' ',
                                                '.',
                                                ',',
                                                '?',
                                                ';',
                                                '!',
                                                ':',
                                                '(',
                                                ')',
                                                '[',
                                                ']',
                                                '{',
                                                '}',
                                                '$',
                                                '"',
                                                '<',
                                                '>',
                                                '"'
                                            }).Select(w => stemmer.Stem(w.Trim()));

                        var words = new BsonArray();
                        allWords
                            .GroupBy(w => w)
                            .ToList()
                            .ForEach(w =>
                                {
                                    var word = new BsonDocument();
                                    word["word"] = w.Key;
                                    word["count"] = w.Count();
                                    words.Add(word);
                                });
                        post["words"] = words;
                        post["theme"] = doc.DocumentNode.SelectNodes(divAdress + "h2/a")[0].InnerText;
                        post["author"] = doc.DocumentNode.SelectNodes(divAdress + "div[1]/div/div[1]/a[2]")[0].InnerText;
                        post["dt"] = new BsonDateTime(DateTime.Parse(doc.DocumentNode.SelectNodes(divAdress + "div[1]/div/div[2]/span[2]")[0].InnerText));
                        var tagsNode = doc.DocumentNode.SelectNodes(divAdress + "div[3]/div[3]/a");
                        if (tagsNode != null)
                        {
                            var tags = new BsonArray();
                            for (var k = 0; k < tagsNode.Count; k++)
                                tags.Add(tagsNode[k].InnerText);
                            post["tags"] = tags;
                        }
                        _posts.Insert(post);
                    }

                    var page = new BsonDocument();
                    page["html"] = responseFromServer;
                    page["url"] = url;
                    _pages.Insert(page);

                    System.Threading.Thread.Sleep(100);
                }
                catch (WebException exeption)
                {
                    MessageBox.Show("All post downloaded!");
                    break;
                }
            }
        }
    }
}