using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Noise.Shared;
using Npgsql;

namespace Noise.SentimentCollection.Engine
{
    class Program
    {
        private static HttpClient NoiseHttpClient = new HttpClient();

        static void Main(string[] args)
        {
            CollectSentiments().Wait();
        }

        private static async Task CollectSentiments()
        {
            // For each scraper configuration (aka TOPIC)
            foreach (RSSScraperConfiguration topicScraper in NoiseConfigurations.ScraperTopics)
            {
                List<string> rssFeedLinks = new List<string>();
                var rssResponseString = "";

                // Make a request to the RSS feed specified in the scraper
                NoiseHttpClient.BaseAddress = new Uri(topicScraper.RSSURL);
                var rssResponseMessage = await NoiseHttpClient.GetAsync(topicScraper.RSSURL);
                rssResponseString = await rssResponseMessage.Content.ReadAsStringAsync();

                // Get a list of article links from the RSS feed response
                XDocument rssFeedResponseXML = XDocument.Parse(rssResponseString);
                foreach (var item in rssFeedResponseXML.Root.Descendants().First(i => i.Name.LocalName == "channel").Elements().Where(i => i.Name.LocalName == "item"))
                    rssFeedLinks.Add(item.Elements().First(i => i.Name.LocalName == topicScraper.LinkLocalName).Value);

                // List of sentiment analysis results from all articles
                List<SentimentInfo> analyzedArticles = new List<SentimentInfo>();

                TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
                DateTime currentPST = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
                string outFile = $"{topicScraper.Topic.ToString()}_{topicScraper.Name}_{currentPST.Year}-{currentPST.Month}-{currentPST.Day}.txt";

                // For each article link
                foreach (var linkURL in rssFeedLinks)
                {
                    SentimentInfo info = await SentimentUtils.MakeRequest(linkURL, NoiseHttpClient, outFile);
                    if (info != null)
                        analyzedArticles.Add(info);
                }

                SentimentInfo consolidatedSentimentInfo = SentimentUtils.ConsolidateSentimentInfo(analyzedArticles);

                // Write sentiments to database
                using (NpgsqlConnection connection = new NpgsqlConnection(NoiseConfigurations.PostgresConnectionString))
                {
                    await connection.OpenAsync();

                    using (NpgsqlCommand command = new NpgsqlCommand())
                    {
                        command.Connection = connection;
                        command.CommandText = @"
                            INSERT INTO sentiments (type, date, valence, domain)
                            VALUES (@type, @date, @valence, @domain)";

                        command.Parameters.AddWithValue("type", (int)topicScraper.Topic);
                        command.Parameters.AddWithValue("date", currentPST);
                        command.Parameters.AddWithValue("valence", consolidatedSentimentInfo.ValenceAverage);
                        command.Parameters.AddWithValue("domain", topicScraper.Name);

                        try
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                        catch (PostgresException ex)
                        {
                            if (ex.SqlState != "23505")
                                throw ex;
                            else
                                Console.WriteLine($"A sentiment has already been collected for {topicScraper.Topic.ToString()} on {currentPST}");
                        }
                    }
                }

                // Write sentiment info to file
                File.AppendAllTextAsync(outFile,
                    $"\n{topicScraper.Topic.ToString()} topic for {currentPST} yielded total valence of {consolidatedSentimentInfo.Valence}, " +
                    $"number of tokens {consolidatedSentimentInfo.NumTokens}, " +
                    $"and average valence of {consolidatedSentimentInfo.ValenceAverage}\n\n").Wait();

                // Write the top 10 proper nouns
                List<KeyValuePair<string, int>> propers = consolidatedSentimentInfo.ProperNounTokens.ToList();
                propers.Sort((p1, p2) => p2.Value.CompareTo(p1.Value));
                await File.AppendAllTextAsync(outFile, "Proper nouns:\n");
                await File.AppendAllLinesAsync(outFile, propers.Take(10).Select(s => $"{s.Key} {s.Value}"));
                await File.AppendAllTextAsync(outFile, "\n");

                // Write the top 10 positive tokens
                List<KeyValuePair<string, int>> positives = consolidatedSentimentInfo.PositiveTokens.ToList();
                positives.Sort((p1, p2) => p2.Value.CompareTo(p1.Value));
                await File.AppendAllTextAsync(outFile, "Positive tokens:\n");
                await File.AppendAllLinesAsync(outFile, positives.Take(10).Select(s => $"{s.Key} {s.Value}"));
                await File.AppendAllTextAsync(outFile, "\n");

                // Write the top 10 negative tokens
                List<KeyValuePair<string, int>> negatives = consolidatedSentimentInfo.NegativeTokens.ToList();
                negatives.Sort((p1, p2) => p2.Value.CompareTo(p1.Value));
                await File.AppendAllTextAsync(outFile, "Negative tokens:\n");
                await File.AppendAllLinesAsync(outFile, negatives.Take(10).Select(s => $"{s.Key} {s.Value}"));
                await File.AppendAllTextAsync(outFile, "\n");
            }
        }
    }
}
