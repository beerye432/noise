﻿using HtmlAgilityPack;
using Noise.Shared;
using Npgsql;
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
        public static async Task<SentimentInfo> MakeRequest(string linkURL, HttpClient client, string outFileName)
        {
            // See if we have information about how to extract information from the domain of the current article
            DomainSettings domain = NoiseConfigurations.KnownDomains.Where(d => linkURL.Contains(d.Domain)).FirstOrDefault();
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
            SentimentInfo info = ProcessText(nodeConcat, NoiseConfigurations.Valences);

            using (NpgsqlConnection connection = new NpgsqlConnection(NoiseConfigurations.PostgresConnectionString))
            {
                await connection.OpenAsync();
                using (NpgsqlCommand command = new NpgsqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = @"
                        INSERT INTO articles (name, published_on, valence)
                        VALUES (@name, @published_on, @valence)";

                    command.Parameters.AddWithValue("name", linkURL);
                    command.Parameters.AddWithValue("published_on", DateTime.UtcNow);
                    command.Parameters.AddWithValue("valence", info.ValenceAverage);

                    try
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                    catch (PostgresException ex)
                    {
                        if (ex.SqlState != "23505")
                            throw ex;
                    }
                }
            }

            await File.AppendAllTextAsync(outFileName, $"Article {linkURL} has total valence of {info.Valence}, number of tokens {info.NumTokens}, and average valence of {info.ValenceAverage}\n");
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
            List<string> tokens = new List<string>(text.Split(" ").Where(s => !string.IsNullOrEmpty(s)));

            // Get proper nouns from list of tokens
            Dictionary<string, int> properNounTokens = GetProperNouns(tokens);

            // Fully strip all non-alphanumeric characters
            Regex fullScrub = new Regex("[^a-zA-Z0-9 ]");
            text = fullScrub.Replace(text, " ");
            text = Regex.Replace(text, @"\s+", " ");

            // Create a new list of tokens, free of any non-alphanumeric characters, ready for rudimentary sentiment analysis
            tokens = new List<string>(text.Split(" "));

            // Go through tokens one by one, seeing if they land in AFINN-en-165 dictionary
            // TODO: refactor this into one of more methods to increase testability
            int valenceTotal = 0;
            int numTokens = 0;
            Dictionary<string, int> positiveTokens = new Dictionary<string, int>();
            Dictionary<string, int> negativeTokens = new Dictionary<string, int>();

            foreach (string token in tokens.Select(t => t.ToLower()))
            {
                // current token appears in valence dictionary
                if (valences.ContainsKey(token))
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

        public static Dictionary<string, int> GetProperNouns(List<string> tokens)
        {
            Dictionary<string, int> properNounTokens = new Dictionary<string, int>();

            // Rudimentary proper known search
            for (int i = 1; i < tokens.Count; i++)
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

                // Let's remove tokens with length 2 that end with ".", i.e middle initials
                tokens.RemoveAll(t => t.Length == 2 && t[1] == '.');

                // If the current token starts with a capital letter and isn't the start of a sentence, keep track of it
                if (char.IsUpper(currentToken[0]) && !tokens[i - 1].Replace(" ", "").EndsWith('.'))
                {
                    string token = currentToken.Replace(".", "");
                    if (NoiseConfigurations.IgnoredProperNouns.Contains(token))
                        continue;

                    // While we're still within bounds of our array, and the NEXT token is a proper noun
                    while ((i + 1) < tokens.Count && char.IsUpper(tokens[i + 1][0]))
                    {
                        string nextToken = tokens[i + 1];
                        if (NoiseConfigurations.IgnoredProperNouns.Contains(nextToken))
                            break;

                        // Append the next proper noun onto the initial token
                        token += " " + nextToken;

                        // Advance our loop
                        i++;
                    }

                    if (properNounTokens.ContainsKey(token))
                        properNounTokens[token]++;
                    else
                        properNounTokens[token] = 1;
                }
            }

            return properNounTokens;
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
