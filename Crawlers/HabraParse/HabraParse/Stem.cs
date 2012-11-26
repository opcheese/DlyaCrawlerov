// -----------------------------------------------------------------------
// <copyright file="Stem.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace HabraParse
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using MongoDB.Bson;
	using MongoDB.Bson.Serialization.Attributes;
	using MongoDB.Bson.Serialization.IdGenerators;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class Stem
	{
		
		public Guid id { get; set; }

		[BsonElementAttribute("word")]
		public string Word { get; set; }

		[BsonElementAttribute("frequency")]
		public long Frequency { get; set; }
	}
}
