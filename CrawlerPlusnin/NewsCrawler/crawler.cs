using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

using HtmlAgilityPack;
using System.Xml.XPath;
using System.Text.RegularExpressions;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;

using Iveonik.Stemmers;

namespace NewsCrawler
{
    public class Crawler : IDisposable
    {
        private string connectionString;
        private MongoRepository repository;
        private Messages mess;
        //без / т.к. все относительные пути с него начинаются
        private string site = "http://lenta.ru"; // "http://www.google.ru"; 

        public Crawler()
        {
            mess = new Messages();
            try
            {
                connectionString = System.Configuration.ConfigurationSettings.AppSettings.Get("localMongoDB");
            }
            catch
            {
                connectionString = "mongodb://localhost/?safe=true";
            }
            try
            {
                repository = new MongoRepository(connectionString, "lentaru", "news");
            }
            catch
            {
                mess.WriteMessage("Сервер БД недоступен");
                throw new Exception("Ошибка в репозитории. Недосупна БД.");
            }
        }

        public void ParseLentaRu()
        {
            //string baseURL = "http://lenta.ru/";
            // /html/body/table[3]/tbody/tr[1]/td[1]
            var mainPage = GetPage(site);
            if (mainPage == null)
            {
                mess.WriteMessage(String.Format("Не удалось загрузить страницу: {0}", site));
                return;
            }

            var nav = mainPage.DocumentNode.SelectNodes("/html/body/table[@class='peredovica'][1]/tr/td[@class='nav']/*/*/a");  //"/html/body/table[3]/tr/td[1]/*/a"); // /html/body/table[3]/tr[1]/td[1]/div[@class='group']");

            foreach (var item in nav)
            {
                string tmp = item.GetAttributeValue("href", "");
                if (tmp == "/columns/")
                    break;
                if (tmp == "/" || !tmp.StartsWith("/"))
                    continue;
                
                PartitionParse(String.Format("{0}{1}", site, tmp));
            }
        }

        private void PartitionParse(string partitionURL)
        {
            if (partitionURL == null)
                return;
             // /html/body/table[3]/tbody/tr/td[2]
            
            var partitionPage = GetPage(partitionURL);
            if (partitionURL == null)
            {
                mess.WriteMessage(String.Format("Не удалось загрузить страницу: {0}", partitionPage));
                return;
            }
            HtmlNode firstNews;
            HtmlNodeCollection newsPageLinks;
            try
            {
                firstNews = partitionPage.DocumentNode.SelectSingleNode("/html/body/table[3]/tr/td[2]/div[2]/h2/a");
                newsPageLinks = partitionPage.DocumentNode.SelectNodes("/html/body/table[3]/tr/td[2]/*/h4/a");
            }
            catch
            {
                ConsoleError.ShowError(String.Format("Ошибка при выделении ссылок в теме: {0}", partitionURL));
                mess.WriteMessage(String.Format("Ошибка при выделении ссылок в теме: {0}", partitionURL));
                return;
            }
            
            if(!repository.ExistDocument(firstNews.InnerText))
            {
                string tmp = firstNews.GetAttributeValue("href", "");
                ParsePage(String.Format("{0}{1}", site, tmp));
            }
            foreach (var item in newsPageLinks)
            {
                if(!repository.ExistDocument(item.InnerText))
                {
                    string tmp = item.GetAttributeValue("href", "");
                    ParsePage(String.Format("{0}{1}", site, tmp));
                }
            }

            //ConsoleError.ShowError("Раздел закончен: "+partitionPage);
            //mess.WriteMessage(String.Format("Сохранили раздел: {0}", partitionPage));  
        }

        public void ParsePage(string urlPage)
        {
            if (urlPage == null)
            {
                mess.WriteMessage(String.Format("Передана пустая ссылка"));
                return;
            }
            Dictionary<string, object> allInformation = new Dictionary<string, object>();
            allInformation.Add("url", urlPage);
            //Регулярка для поиска начала статьи. Ограничение на количество, что бы не зацепить другие комменты
            Regex beginText = new Regex("<!--.*testcom.{0,5}news.{0,50}-->");
            Regex beginText2 = new Regex("<!-- СТАТЬЯ -->");
            //Регулярка для поиска конца статьи
            Regex endText = new Regex("(Ссылки по теме)|(Сайты по теме)|(<!-- social -->)");

            //urlPage = "http://lenta.ru/news/2012/10/07/milf/"; // "http://lenta.ru/news/2012/10/01/party/"; //http://lenta.ru/news/2012/10/05/manson1/
            DateTime dateArticle = ConvertSubUriInDateTime(urlPage);
            allInformation.Add("date", dateArticle);

            string subUrl = string.Format("testcom/news/2012");

            HtmlDocument newsPage = GetPage(urlPage);
            if (newsPage == null)
            {
                mess.WriteMessage(String.Format("Не удалось загрузить страницу: {0}", urlPage));
                return;
            }

            allInformation.Add("articleHtml", newsPage.DocumentNode.InnerHtml);

            var text = newsPage.DocumentNode.SelectSingleNode("//td[@class='statya']");
            var title = newsPage.DocumentNode.SelectSingleNode("/html/head/title");
            var description = newsPage.DocumentNode.SelectSingleNode("/html/head/meta[1]");
            string descrTest = description.GetAttributeValue("content", "");
            allInformation.Add("description", descrTest);

            string badText = text.InnerText;
            var matchBegin = beginText.Match(badText);
            if (!matchBegin.Success)
                matchBegin = beginText2.Match(badText);
            var matchEnd = endText.Match(badText);
            if (!matchBegin.Success || !matchEnd.Success)
            {
                mess.WriteMessage(String.Format("Не удалось выделить основной текст: {0}", urlPage));
                //throw new Exception("Начало или конец статьи не найден!");
            }
            StringBuilder articleText = new StringBuilder(
                badText.Substring(matchBegin.Index + matchBegin.Length, matchEnd.Index - matchBegin.Index - matchBegin.Length + 0));
            //после замены чистый текст статьи
            articleText.Replace("\n", "");

            allInformation.Add("articleText", articleText.ToString());

            //выделение основ
            RussianStemmer rusStemmer = new RussianStemmer();
            Regex separators = new Regex("[А-Яа-я]+");
            var matches = separators.Matches(articleText.ToString());
            Dictionary<string, int> words = new Dictionary<string, int>();
            foreach (Match item in matches)
            {
                string tmp = rusStemmer.Stem(item.Value);
                if (words.ContainsKey(tmp))
                    words[tmp]++;
                else
                    words.Add(tmp, 1);
            }

            //Выделение тематики
            Regex themeRegEx = new Regex(":");
            var themeMatches = themeRegEx.Matches(title.InnerText);
            string theme = title.InnerText.Substring(themeMatches[0].Index+1, themeMatches[1].Index - themeMatches[0].Index - 1).Trim();

            allInformation.Add("theme", theme);

            //Выделение заголовка
            var h2Title = newsPage.DocumentNode.SelectSingleNode("/html/body/table[@class='peredovica']/tr[1]/td[@id='pacman']/table/tr/td/h2");
            string titleText = h2Title.InnerText;

            allInformation.Add("title", titleText);

            //Выделение ссылок по теме
            //*[@id="pacman"]/table/tbody/tr/td/p[6]
            //*[@id="pacman"]/table/tbody/tr/td/p[6]/a
            var linksOnTheme = newsPage.DocumentNode.SelectNodes("/html/body/table[@class='peredovica']/tr[1]/td[@id='pacman']/table/tr/td/p[@class='links']/a");
            List<string> links = new List<string>();
            if (linksOnTheme != null)
            {
                foreach (var item in linksOnTheme)
                {
                    string tmp = item.GetAttributeValue("href", "");
                    if (tmp != String.Empty)
                        links.Add(tmp);
                }
            }

            allInformation.Add("wordBase", words);

            allInformation.Add("themeLinks", links);

            //Выделение ссылок в тексте
            var linksInArticle = newsPage.DocumentNode.SelectNodes("/html/body/table[@class='peredovica']/tr[1]/td[@id='pacman']/table/tr/td/a");
            List<string> listLinksInArticle = new List<string>();

            if (linksInArticle != null)
            {
                foreach (var item in linksInArticle)
                {
                    string tmp = item.GetAttributeValue("href", "");
                    if (!tmp.StartsWith("http://lenta.ru"))
                        tmp = String.Format("http://lenta.ru{0}", tmp);
                    if(tmp != String.Empty)
                        listLinksInArticle.Add(tmp);
                }
            }

            allInformation.Add("articleLinks", listLinksInArticle);

            //сохраняем в Mongo
            repository.SaveDocument(allInformation);

            mess.WriteMessage(String.Format("Сохранили: {0}", urlPage));            
        }

        private DateTime ConvertSubUriInDateTime(string subUrl)
        {
            Regex regExDate = new Regex("\\d{4}/\\d{2}/\\d{2}");
            var dateMatch = regExDate.Match(subUrl);
            if (!dateMatch.Success)
                throw new Exception("В строке даты не найдено");
            string[] date = dateMatch.Value.Split('/');
            DateTime res;
            try
            {
                res = new DateTime(int.Parse(date[0]), int.Parse(date[1]), int.Parse(date[2]));
            }
            catch
            {
                mess.WriteMessage(String.Format("Не удалось преобразоваь дату"));
                return new DateTime();
                //throw new Exception("Не удалось преобразовать дату");
            }
            return res;
        }

        /// <summary>
        /// Загружает страницу по адресу
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Возвращает страницу HtmlDocument или null с выводом ошибки</returns>
        private HtmlDocument GetPage(string url)
        {
            //WebClient client = new WebClient();
            //client.Encoding = Encoding.UTF8;
            //client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2)");
            //Stream stream = client.OpenRead(url);
            //StreamReader data = new StreamReader(stream);
            //string html = data.ReadToEnd();
            //wc.OpenRead();
            HtmlDocument doc = new HtmlDocument();
            //doc.LoadHtml(html);

            HtmlWeb page = new HtmlWeb();
            //page.OverrideEncoding = Encoding.Default;
            //page.UsingCache = false;
            page.AutoDetectEncoding = true;
            page.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2)";
            page.OverrideEncoding = Encoding.Default;

            HtmlDocument newsPage = new HtmlDocument();
            
            try
            {
                newsPage = page.Load(url);
                
            }
            catch 
            {
                ConsoleError.ShowError(String.Format("Не удалось загрузить страницу: {0}", url));
                mess.WriteMessage(String.Format("Не удалось загрузить страницу: {0}", url));
                //throw new Exception();
                return null;
            }

            return newsPage;

        }

        public void Dispose()
        {
            mess.Dispose();
        }
    }
}
