using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using HtmlAgilityPack;

namespace Noise.SentimentCollection.Engine
{
    class Program
    {
        static void Main(string[] args)
        {
            CollectSentiments().Wait();
        }

        private static async Task CollectSentiments()
        {
            // Load scraper configuration
            string jsonString = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "scraperconfig.json"));
            List<RSSScraperConfiguration> topics = JsonConvert.DeserializeObject<List<RSSScraperConfiguration>>(jsonString);

            // Load domain settings
            jsonString = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "domainsettings.json"));
            List<DomainSettings> knownDomains = JsonConvert.DeserializeObject<List<DomainSettings>>(jsonString);

            // Create valence dictionary for NLP
            Dictionary<string, int> valences = await ValenceDictionaryUtils.CreateValenceDictionary();

            // For each scraper configuration (aka TOPIC)
            foreach (RSSScraperConfiguration topicScraper in topics)
            {
                List<string> rssFeedLinks = new List<string>();
                var rssResponseString = "";
                using (var client = new HttpClient())
                {
                    // Make a request to the RSS feed specified in the scraper
                    client.BaseAddress = new Uri(topicScraper.RSSURL);
                    var rssResponseMessage = await client.GetAsync(topicScraper.RSSURL);
                    rssResponseString = await rssResponseMessage.Content.ReadAsStringAsync();

                    // Get a list of article links from the RSS feed response
                    XDocument rssFeedResponseXML = XDocument.Parse(rssResponseString);
                    foreach (var item in rssFeedResponseXML.Root.Descendants().First(i => i.Name.LocalName == "channel").Elements().Where(i => i.Name.LocalName == "item"))
                        rssFeedLinks.Add(item.Elements().First(i => i.Name.LocalName == topicScraper.LinkLocalName).Value);

                    // List of sentiment analysis results from all articles
                    List<SentimentInfo> analyzedArticles = new List<SentimentInfo>();

                    // For each article link
                    foreach (var linkURL in rssFeedLinks)
                    {
                        SentimentInfo info = await SentimentUtils.MakeRequest(linkURL, client, valences, knownDomains);
                        if(info != null)
                            analyzedArticles.Add(info);
                    }

                    TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
                    DateTime currentPST = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

                    SentimentInfo consolidatedSentimentInfo = SentimentUtils.ConsolidateSentimentInfo(analyzedArticles);
                    File.AppendAllTextAsync("out.txt",
                        $"\n{topicScraper.Topic.ToString()} topic for {currentPST} yielded total valence of {consolidatedSentimentInfo.Valence}, " +
                        $"number of tokens {consolidatedSentimentInfo.NumTokens}, " +
                        $"and average valence of {consolidatedSentimentInfo.ValenceAverage}\n\n").Wait();

                    List<KeyValuePair<string, int>> propers = consolidatedSentimentInfo.ProperNounTokens.ToList();
                    propers.Sort((p1, p2) => p2.Value.CompareTo(p1.Value));
                    await File.AppendAllTextAsync("out.txt", "Proper nouns:\n");
                    await File.AppendAllLinesAsync("out.txt", propers.Select(s => $"{s.Key} {s.Value}"));
                    await File.AppendAllTextAsync("out.txt", "\n");

                    List<KeyValuePair<string, int>> positives = consolidatedSentimentInfo.PositiveTokens.ToList();
                    positives.Sort((p1, p2) => p2.Value.CompareTo(p1.Value));
                    await File.AppendAllTextAsync("out.txt", "Positive tokens:\n");
                    await File.AppendAllLinesAsync("out.txt", positives.Select(s => $"{s.Key} {s.Value}"));
                    await File.AppendAllTextAsync("out.txt", "\n");

                    List<KeyValuePair<string, int>> negatives = consolidatedSentimentInfo.NegativeTokens.ToList();
                    negatives.Sort((p1, p2) => p2.Value.CompareTo(p1.Value));
                    await File.AppendAllTextAsync("out.txt", "Negative tokens:\n");
                    await File.AppendAllLinesAsync("out.txt", negatives.Select(s => $"{s.Key} {s.Value}"));
                    await File.AppendAllTextAsync("out.txt", "\n");
                }
            }
        }
    }
}
