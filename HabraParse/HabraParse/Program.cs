using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using System.Net;

namespace HabraParse
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.Write("Command: ");
			string cmd = Console.ReadLine();
			Crawler crawler = new Crawler();
			if (cmd == "download")
			{
				crawler.DownloadNew();
			}
			else if (cmd == "reload")
				crawler.ReloadExisting();
		}
	}
}
