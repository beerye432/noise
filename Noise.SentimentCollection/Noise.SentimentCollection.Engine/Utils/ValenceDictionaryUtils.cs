using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Noise.SentimentCollection.Engine
{
    public class ValenceDictionaryUtils
    {
        private static readonly string AFINN_FILENAME = "AFINN-en-165.txt";

        public static async Task<Dictionary<string, int>> CreateValenceDictionary()
        {
            Dictionary<string, int> valenceDictionary = new Dictionary<string, int>();

            using (StreamReader reader = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), AFINN_FILENAME)))
            {
                string line = "";
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    string[] info = line.Split(null);
                    try
                    {
                        valenceDictionary.Add(info[0], int.Parse(info[1]));
                    }
                    catch(Exception ex)
                    {
                        Console.Write(ex.Message);
                    }
                }
            }
            return valenceDictionary;
        }
    }
}
