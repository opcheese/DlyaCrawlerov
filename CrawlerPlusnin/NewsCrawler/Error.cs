using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace NewsCrawler
{
    public static class ConsoleError
    {
        public static void ShowError(string error)
        {
            using (StreamWriter file = new StreamWriter(new FileStream("error.txt", FileMode.OpenOrCreate, FileAccess.Write)))
            {
                file.WriteLine(error);
                file.Flush();
            }
            
            Console.WriteLine("{0}", 0);
        }
    }

    public class Messages : IDisposable
    {
        private StreamWriter file;

        public Messages()
        {
            file = File.CreateText("messages.txt");
        }

        public void WriteMessage(string msg)
        {
            if (msg != null)
            {
                file.WriteLine(msg);
                Console.WriteLine(msg);
            }
        }

        public void Dispose()
        {
            file.Flush();
            file.Close();
        }
    }

}
