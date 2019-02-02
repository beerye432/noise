using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Noise.SentimentCollection.Engine
{
    public static class SentimentUtils
    {
        public static SentimentInfo ProcessText(string text, Dictionary<string, int> valences)
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

            HashSet<string> properNounTokens = new HashSet<string>();

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
                    properNounTokens.Add(currentToken.Replace(".", "").ToLower());
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
            HashSet<string> positiveTokens = new HashSet<string>();
            HashSet<string> negativeTokens = new HashSet<string>();
            HashSet<string> neutralTokens = new HashSet<string>();

            foreach (string token in tokens.Select(t => t.ToLower()))
            {
                // current token appears in valence dictionary
                if(valences.ContainsKey(token))
                {
                    int valence = valences[token];

                    if (valence > 0) // token is positive
                        positiveTokens.Add(token);
                    else if (valence == 0) // token is neutral
                        neutralTokens.Add(token);
                    else // token is negative
                        negativeTokens.Add(token);

                    // Add token's valence to valence total
                    valenceTotal += valences[token];

                    // Increment number of identified tokens
                    numTokens++;
                }
            }

            SentimentInfo info = new SentimentInfo(valenceTotal, numTokens)
            {
                PositiveTokens = positiveTokens,
                NeutralTokens = neutralTokens,
                NegativeTokens = negativeTokens,
                ProperNounTokens = properNounTokens
            };

            return info;
        }

        public static SentimentInfo ConsolidateSentimentInfo(List<SentimentInfo> sentiments)
        {
            HashSet<string> consolidatedProperNouns = new HashSet<string>();
            foreach (HashSet<string> properNouns in sentiments.Select(s => s.ProperNounTokens))
                consolidatedProperNouns.UnionWith(properNouns);

            HashSet<string> consolidatedPositiveTokens = new HashSet<string>();
            foreach (HashSet<string> properNouns in sentiments.Select(s => s.PositiveTokens))
                consolidatedPositiveTokens.UnionWith(properNouns);

            HashSet<string> consolidatedNeutralTokens = new HashSet<string>();
            foreach (HashSet<string> properNouns in sentiments.Select(s => s.NeutralTokens))
                consolidatedNeutralTokens.UnionWith(properNouns);

            HashSet<string> consolidatedNegativeTokens = new HashSet<string>();
            foreach (HashSet<string> properNouns in sentiments.Select(s => s.NegativeTokens))
                consolidatedNegativeTokens.UnionWith(properNouns);

            return new SentimentInfo(sentiments.Sum(s => s.Valence), sentiments.Sum(s => s.NumTokens))
            {
                ProperNounTokens = consolidatedProperNouns,
                NeutralTokens = consolidatedNeutralTokens,
                NegativeTokens = consolidatedNegativeTokens,
                PositiveTokens = consolidatedPositiveTokens
            };
        }
    }
}
