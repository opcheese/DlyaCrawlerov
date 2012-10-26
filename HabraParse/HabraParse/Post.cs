// -----------------------------------------------------------------------
// <copyright file="Post.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace HabraParse
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using MongoDB.Bson.Serialization.Attributes;
	using HtmlAgilityPack;
	using System.Globalization;
	using Iveonik.Stemmers;
	using System.Text.RegularExpressions;
	using MongoDB.Bson.Serialization.IdGenerators;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>	
	public class Post
	{
		public string id { get; set; }

		[BsonElementAttribute("published")]
		public DateTime Published { get; set; }

		[BsonElementAttribute("url")]
		public string Url { get; set; }

		[BsonElementAttribute("title")]
		public string Title { get; set; }

		[BsonElementAttribute("hubs")]
		public List<string> Hubs { get; set; }

		[BsonElementAttribute("posthtml")]
		public string PostHtml { get; set; }

		[BsonElementAttribute("posttext")]
		public string PostText { get; set; }

		[BsonElementAttribute("tags")]
		public List<string> Tags { get; set; }

		[BsonElementAttribute("pageviews")]
		public int PageViews { get; set; }

		[BsonElementAttribute("commentscount")]
		public int CommentsCount { get; set; }

		[BsonElementAttribute("favoritescount")]
		public int FavoritesCount { get; set; }

		[BsonElementAttribute("author")]
		public string Author { get; set; }

		[BsonElementAttribute("authorrating")]
		public string AuthorRating { get; set; }

		[BsonElementAttribute("comments")]
		public List<Comment> Comments { get; set; }

		[BsonElementAttribute("translation")]
		public bool Translation { get; set; }

		[BsonElementAttribute("originalauthor")]
		public string OriginalAuthor { get; set; }

		[BsonElementAttribute("originalsource")]
		public string OriginalSource { get; set; }

		[BsonElementAttribute("stems")]
		public List<Stem> Stems { get; set; }

		public static Post Parse(HtmlNode postContentHtmlnode)
		{
			if (postContentHtmlnode != null)
			{
				string published = postContentHtmlnode.SelectSingleNode("//div[@class='published']").InnerText;
				CultureInfo provider = CultureInfo.CurrentCulture;
				string format = "d MMMM yyyy в HH:mm";
				DateTime publishedDate = DateTime.ParseExact(published.Trim(), format, provider, DateTimeStyles.AllowWhiteSpaces);

				string title = postContentHtmlnode.SelectSingleNode("//span[@class='post_title']").InnerText;
				var hubnodes = postContentHtmlnode.SelectNodes("//a[@class='hub ']");
				List<string> hubs = new List<string>();
				if (hubnodes != null)
					hubs = hubnodes.Select(x => x.InnerText).ToList();
				string postHtml = postContentHtmlnode.SelectSingleNode("//div[@class='content html_format']").InnerHtml;
				string postText = postContentHtmlnode.SelectSingleNode("//div[@class='content html_format']").InnerText;

				Regex wordsRegex = new Regex(@"\w+");
				RussianStemmer stemmer = new RussianStemmer();
				var stems = wordsRegex
					.Matches(postText)
					.Cast<Match>()
					.Select(x => stemmer.Stem(x.Value))
					.GroupBy(x => x)
					.Select(x => new Stem() { id = Guid.NewGuid(), Word = x.Key, Frequency = x.Count() });


				var tagNodes = postContentHtmlnode.SelectSingleNode("//ul[@class='tags']").SelectNodes("//a[@rel='tag']");
				List<string> tags = new List<string>();
				if (tagNodes != null)
					tags = tagNodes.Select(x => x.InnerText).ToList();
				string pageViews = postContentHtmlnode.SelectSingleNode("//div[@class='pageviews']").InnerText;
				if (string.IsNullOrEmpty(pageViews))
					pageViews = "0";
				string favCount = postContentHtmlnode.SelectSingleNode("//div[@class='favs_count']").InnerText;
				if (string.IsNullOrEmpty(favCount))
					favCount = "0";
				string author = postContentHtmlnode.SelectSingleNode("//div[@class='author']").SelectSingleNode(".//a").InnerText;
				string authorRating = postContentHtmlnode.SelectSingleNode("//span[@class='rating']").InnerText;
				string commentsCount = postContentHtmlnode.SelectSingleNode("//span[@id='comments_count']").InnerText;
				if (string.IsNullOrEmpty(commentsCount))
					commentsCount = "0";
				string postId = postContentHtmlnode.SelectSingleNode("//div[@class='post shortcuts_item'] | //div[@class='post translation shortcuts_item']").GetAttributeValue("id", null);
				postId = postId.Split('_')[1];

				string originalAuthor = null;
				string originalSource = null;
				bool translation = false;
				var originalAuthorNode = postContentHtmlnode.SelectSingleNode("//div[@class='original-author']/a");
				if (originalAuthorNode != null)
				{
					translation = true;
					originalAuthor = originalAuthorNode.InnerText;
					originalSource = originalAuthorNode.GetAttributeValue("href", null);
				}


				List<Comment> comments = new List<Comment>();
				var commentNodes = postContentHtmlnode.SelectNodes("//div[@class='comment_item']");
				if (commentNodes != null)
					foreach (var commentNode in commentNodes)
					{
						Comment comment = Comment.Parse(commentNode, postId);
						if (comment != null)
							comments.Add(comment);
					}

				return new Post
				{
					Author = author,
					AuthorRating = authorRating,
					CommentsCount = int.Parse(commentsCount),
					Comments = comments,
					FavoritesCount = int.Parse(favCount),
					Hubs = hubs,
					id = postId,
					PageViews = int.Parse(pageViews),
					PostHtml = postHtml,
					PostText = postText,
					Published = publishedDate,
					Tags = tags,
					Title = title,
					OriginalAuthor = originalAuthor,
					OriginalSource = originalSource,
					Translation = translation,
					Stems = new List<Stem>(stems)
				};
			}
			else
				return null;
		}
	}
}
