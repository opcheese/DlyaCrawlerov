using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

using NewsCrawler;

namespace startCrawler
{
    class Program
    {
        static void Main(string[] args)
        {

            try
            {
                using (Crawler cr = new Crawler())
                {
                    //cr.ParsePage("http://lenta.ru/news/2012/10/08/win/"); //.ParsePage("http://lenta.ru/news/2012/10/01/party/");
                    //cr.PartitionParse("http://lenta.ru/world/");
                    //cr.ParsePage("http://lenta.ru/news/2012/10/08/shushary/");
                    cr.ParseLentaRu();
                }
            }
            catch
            {
                Console.WriteLine("Ошибка.");
            }
            Console.WriteLine("Все новые новости в базе :-) Сморим лог в messages.txt");
            Console.ReadLine();
        }
    }
}
