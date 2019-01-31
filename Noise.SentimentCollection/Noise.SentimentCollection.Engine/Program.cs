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
                        // See if we have information about how to extract information from the domain of the current article
                        DomainSettings domain = knownDomains.Where(d => linkURL.Contains(d.Domain)).FirstOrDefault();
                        if (domain == null)
                            continue;

                        // Make a GET request for the current article
                        var article = await client.GetAsync(linkURL);
                        var articleResponse = await article.Content.ReadAsStringAsync();

                        // Create HTML document from HTTP response
                        var articleHTML = new HtmlDocument();
                        articleHTML.LoadHtml(articleResponse);

                        // Select nodes that conform to the domain's relevant element type and class
                        HtmlNodeCollection newsSnippets = articleHTML.DocumentNode.SelectNodes($"//{domain.RelevantElementType}[contains(@class, '{domain.RelevantClassName}')]");

                        // Smush all the relevant nodes' inner text into 1 big string (adding spaces between nodes)
                        string nodeConcat = string.Concat(newsSnippets.Select(n => n.InnerText + " "));

                        // Feed concatenated article into processor
                        SentimentInfo info = SentimentUtils.ProcessText(nodeConcat, valences);
                        analyzedArticles.Add(info);

                        await File.AppendAllTextAsync("out.txt", $"Article {linkURL} has total valence of {info.Valence}, number of tokens {info.NumTokens}, and average valence of {info.ValenceAverage}\n");
                    }
                }
            }
        }
    }
}
