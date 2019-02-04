using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Noise.SentimentCollection.Engine
{
    public static class SentimentUtils
    {
        public static async Task<SentimentInfo> MakeRequest(string linkURL, HttpClient client, Dictionary<string, int> valences, List<DomainSettings> knownDomains)
        {
            // See if we have information about how to extract information from the domain of the current article
            DomainSettings domain = knownDomains.Where(d => linkURL.Contains(d.Domain)).FirstOrDefault();
            if (domain == null)
                return null;

            // Make a GET request for the current article
            var article = await client.GetAsync(linkURL);
            var articleResponse = await article.Content.ReadAsStringAsync();

            // Create HTML document from HTTP response
            var articleHTML = new HtmlDocument();
            articleHTML.LoadHtml(articleResponse);

            // Select nodes that conform to the domain's relevant element type and class
            HtmlNodeCollection newsSnippets = articleHTML.DocumentNode.SelectNodes($"//{domain.RelevantElementType}[contains(@class, '{domain.RelevantClassName}')]");

            // No nodes that conform to domain's relevant elemnt type and class? skip
            if (newsSnippets == null || newsSnippets.Count == 0)
                return null;

            // Smush all the relevant nodes' inner text into 1 big string (adding spaces between nodes)
            string nodeConcat = string.Concat(newsSnippets.Select(n => n.InnerText + " "));

            // Feed concatenated article into processor
            SentimentInfo info = SentimentUtils.ProcessText(nodeConcat, valences);
            await File.AppendAllTextAsync("out.txt", $"Article {linkURL} has total valence of {info.Valence}, number of tokens {info.NumTokens}, and average valence of {info.ValenceAverage}\n");
            return info;

        }

        private static SentimentInfo ProcessText(string text, Dictionary<string, int> valences)
        {
            // Remove <a> anchor tags
            Regex anchorTagRemove = new Regex("<a[*]a>");
            text = anchorTagRemove.Replace(text, "");

            // Replace non-alphanumeric characters (except periods) with spaces, to preserve ability to split
            Regex removeNonAlphaNumeric = new Regex("[^a-zA-Z0-9. ]");
            text = removeNonAlphaNumeric.Replace(text, " ");

            // Replace any instance with 2 or more spaces with 1 space
            text = Regex.Replace(text, @"\s+", " ");

            // Split the scrubbed text into tokens
            List<string> tokens = new List<string>(text.Split(" "));

            Dictionary<string, int> properNounTokens = new Dictionary<string, int>();

            // Rudimentary proper known search
            for(int i = 1; i < tokens.Count; i++)
            {
                /*
                 * Search for tokens with a capital first letter.
                 * If the token before this token ends with a period, 
                 * the current token is capitalized because it is the start of a new sentence,
                 * and should be ignored. Start at 1 to ignore first word in an article
                 */
                string currentToken = tokens[i];

                if (string.IsNullOrEmpty(currentToken))
                    continue;

                if (char.IsUpper(currentToken[0]) && !tokens[i - 1].Replace(" ", "").EndsWith('.'))
                {
                    string token = currentToken.Replace(".", "").ToLower();
                    if (properNounTokens.ContainsKey(token))
                        properNounTokens[token]++;
                    else
                        properNounTokens[token] = 1;
                }
            }

            // Fully strip all non-alphanumeric characters
            Regex fullScrub = new Regex("[^a-zA-Z0-9 ]");
            text = fullScrub.Replace(text, " ");
            text = Regex.Replace(text, @"\s+", " ");

            // Create a new list of tokens, free of any non-alphanumeric characters, ready for rudimentary sentiment analysis
            tokens = new List<string>(text.Split(" "));

            // Go through tokens one by one, seeing if they land in AFINN-en-165 dictionary
            int valenceTotal = 0;
            int numTokens = 0;
            Dictionary<string, int> positiveTokens = new Dictionary<string, int>();
            Dictionary<string, int> negativeTokens = new Dictionary<string, int>();

            foreach (string token in tokens.Select(t => t.ToLower()))
            {
                // current token appears in valence dictionary
                if(valences.ContainsKey(token))
                {
                    int valence = valences[token];

                    if (valence > 0) // token is positive
                    {
                        if (positiveTokens.ContainsKey(token))
                            positiveTokens[token]++;
                        else
                            positiveTokens[token] = 1;
                    }
                    else // token is negative
                    {
                        if (negativeTokens.ContainsKey(token))
                            negativeTokens[token]++;
                        else
                            negativeTokens[token] = 1;
                    }

                    // Add token's valence to valence total
                    valenceTotal += valences[token];

                    // Increment number of identified tokens
                    numTokens++;
                }
            }

            SentimentInfo info = new SentimentInfo(valenceTotal, numTokens)
            {
                PositiveTokens = positiveTokens,
                NegativeTokens = negativeTokens,
                ProperNounTokens = properNounTokens
            };

            return info;
        }

        public static SentimentInfo ConsolidateSentimentInfo(List<SentimentInfo> sentiments)
        {
            Dictionary<string, int> consolidatedProperNouns = new Dictionary<string, int>();
            foreach (Dictionary<string, int> properNouns in sentiments.Select(s => s.ProperNounTokens))
            {
                foreach(KeyValuePair<string, int> properNoun in properNouns)
                {
                    if (consolidatedProperNouns.ContainsKey(properNoun.Key))
                        consolidatedProperNouns[properNoun.Key] += properNoun.Value;
                    else
                        consolidatedProperNouns[properNoun.Key] = 1;
                }
            }

            Dictionary<string, int> consolidatedNegativeTokens = new Dictionary<string, int>();
            foreach (Dictionary<string, int> negativeTokens in sentiments.Select(s => s.NegativeTokens))
            {
                foreach (KeyValuePair<string, int> negativeToken in negativeTokens)
                {
                    if (consolidatedNegativeTokens.ContainsKey(negativeToken.Key))
                        consolidatedNegativeTokens[negativeToken.Key] += negativeToken.Value;
                    else
                        consolidatedNegativeTokens[negativeToken.Key] = 1;
                }
            }

            Dictionary<string, int> consolidatedPositiveTokens = new Dictionary<string, int>();
            foreach (Dictionary<string, int> positiveTokens in sentiments.Select(s => s.PositiveTokens))
            {
                foreach (KeyValuePair<string, int> positiveToken in positiveTokens)
                {
                    if (consolidatedPositiveTokens.ContainsKey(positiveToken.Key))
                        consolidatedPositiveTokens[positiveToken.Key] += positiveToken.Value;
                    else
                        consolidatedPositiveTokens[positiveToken.Key] = 1;
                }
            }

            return new SentimentInfo(sentiments.Sum(s => s.Valence), sentiments.Sum(s => s.NumTokens))
            {
                ProperNounTokens = consolidatedProperNouns,
                NegativeTokens = consolidatedNegativeTokens,
                PositiveTokens = consolidatedPositiveTokens
            };
        }
    }
}
