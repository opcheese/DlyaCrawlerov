// -----------------------------------------------------------------------
// <copyright file="Comment.cs" company="">
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
	using System.Text.RegularExpressions;
	using Iveonik.Stemmers;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class Comment
	{
		public string id { get; set; }

		[BsonElementAttribute("parentid")]
		public string ParentId { get; set; }

		[BsonElementAttribute("score")]
		public int Score { get; set; }

		[BsonElementAttribute("user")]
		public string User { get; set; }

		[BsonElementAttribute("time")]
		public DateTime Time { get; set; }

		[BsonElementAttribute("postid")]
		public string PostId { get; set; }

		[BsonElementAttribute("commenttext")]
		public string Text { get; set; }

		[BsonElementAttribute("stems")]
		public List<Stem> Stems { get; set; }

		public static Comment Parse(HtmlNode commentNode, string postId)
		{
			if (commentNode != null)
			{
				string commentId = commentNode.GetAttributeValue("id", null);
				if (commentId != null)
				{
					commentId = commentId.Split('_')[1];
					string parentId = commentNode.SelectSingleNode("./span[@class='parent_id']").GetAttributeValue("data-parent_id", null);
					string user = commentNode.SelectSingleNode("./div[@class='info  ']/a[@class='username']").InnerText;
					string score = commentNode.SelectSingleNode("./div[@class='info  ']/div[@class='voting   ']/*/span[@class='score']").InnerText;

					string text = commentNode.SelectSingleNode("./div[contains(@class,'message html_format ')]").InnerText.Trim();
					Regex wordsRegex = new Regex(@"\w+");
					RussianStemmer stemmer = new RussianStemmer();
					var stems = wordsRegex
						.Matches(text)
						.Cast<Match>()
						.Select(x => stemmer.Stem(x.Value))
						.GroupBy(x => x)
						.Select(x => new Stem() { id = Guid.NewGuid(), Word = x.Key, Frequency = x.Count() });

					string timeStr = commentNode.SelectSingleNode("./div[@class='info  ']/time").InnerText;
					CultureInfo provider = CultureInfo.CurrentCulture;
					string[] formats = new string[] { "d MMMM yyyy в HH:mm", "d MMMM yyyy в HH:mm (комментарий был изменён)" };
					DateTime time = DateTime.ParseExact(timeStr, formats, provider, DateTimeStyles.AllowWhiteSpaces);
					if (string.IsNullOrEmpty(score))
						score = "0";
					score = score.Trim('+');
					score = score.Replace("–", "-");

					return new Comment()
					{
						id = commentId,
						ParentId = (parentId == null || parentId == "0") ? null : parentId,
						PostId = postId,
						Score = int.Parse(score),
						Time = time,
						User = user,
						Text = text,
						Stems = new List<Stem>(stems)
					};


				}
				else
					return null;
			}
			else
				return null;
		}
	}
}
