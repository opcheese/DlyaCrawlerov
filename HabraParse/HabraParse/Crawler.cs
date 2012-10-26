// -----------------------------------------------------------------------
// <copyright file="Crawler.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace HabraParse
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using MongoDB.Driver.Builders;
	using MongoDB.Driver.GridFS;
	using MongoDB.Driver.Linq;
	using MongoDB.Bson;
	using MongoDB.Driver;
	using HtmlAgilityPack;
	using System.Net;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class Crawler
	{
		const int NEEDED_POST_COUNT = 2;
		int downloadedCount = 0;
		int reloadedCount = 0;
		long existedBeforeCount = 0;
		string connectionString;
		MongoServer server;
		MongoDatabase habraDb;
		WebClient client;
		MongoCollection<Post> posts;

		public void DownloadNew()
		{
			connectionString = "mongodb://localhost/?safe=true";
			server = MongoServer.Create(connectionString);
			habraDb = server.GetDatabase("habr", SafeMode.True);
			posts = habraDb.GetCollection<Post>("posts");
			existedBeforeCount = posts.Count();

			int postListPage = 1;
			while (downloadedCount < NEEDED_POST_COUNT)
			{
				using (client = new WebClient())
				{
					client.Encoding = Encoding.UTF8;
					try
					{
						string url = "http://habrahabr.ru/posts/collective/";
						if (postListPage > 1)
							url += string.Format("page{0}/", postListPage);
						string pageContent = client.DownloadString(url);
						ParsePostListPage(pageContent);
						postListPage++;
					}
					catch { }
				}
			}
		}

		public void ReloadExisting()
		{
			connectionString = "mongodb://localhost/?safe=true";
			server = MongoServer.Create(connectionString);
			habraDb = server.GetDatabase("habr", SafeMode.True);
			posts = habraDb.GetCollection<Post>("posts");
			existedBeforeCount = posts.Count();

			using (client = new WebClient())
			{
				client.Encoding = Encoding.UTF8;
				foreach (var post in posts.FindAll())
				{
					try
					{
						Console.Clear();
						Console.WriteLine(string.Format("Reloaded: {0} posts of {1} possible", reloadedCount, existedBeforeCount));
						Console.WriteLine("Processing: " + post.Url);
						Console.WriteLine("Loading...");
						string pageContent = client.DownloadString(post.Url);
						Console.WriteLine("Parsing...");
						ParsePostPage(post.Url, pageContent);

					}
					catch { }
				}
			}
		}

		private void ParsePostListPage(string pageContent)
		{
			if (pageContent == null)
				return;

			HtmlDocument docPostList = new HtmlDocument();
			docPostList.LoadHtml(pageContent);

			foreach (var postNode in docPostList.DocumentNode.SelectNodes(@"//div[@class='posts shortcuts_items']/div"))
			{
				string postid = postNode.GetAttributeValue("id", null);
				if (postid != null)
				{
					string postPageContent = null;
					try
					{
						string url = string.Format("http://habrahabr.ru/{0}/", postid.Replace('_', '/'));
						Console.Clear();
						Console.WriteLine(string.Format("Downloaded: {0} posts of {1} needed", downloadedCount, NEEDED_POST_COUNT));
						Console.WriteLine(string.Format("Reloaded: {0} posts of {1} possible", reloadedCount, existedBeforeCount));
						Console.WriteLine("Processing: " + url);
						Console.WriteLine("Loading...");
						postPageContent = client.DownloadString(url);
						Console.WriteLine("Parsing...");
						ParsePostPage(url, postPageContent);
					}
					catch { }
				}
			}
		}

		private void ParsePostPage(string url, string pageContent)
		{
			if (pageContent == null)
				return;

			HtmlDocument docPost = new HtmlDocument();
			docPost.LoadHtml(pageContent);
			var contentNode = docPost.DocumentNode.SelectSingleNode("//div[@class='content_left']");
			Post post = Post.Parse(contentNode);

			if (post != null)
			{
				post.Url = url;
				var result = posts.Update(Query.EQ("_id", post.id), Update.Replace<Post>(post), UpdateFlags.Upsert);
				if (!result.UpdatedExisting)
					downloadedCount++;
				else
					reloadedCount++;
			}
		}
	}
}
