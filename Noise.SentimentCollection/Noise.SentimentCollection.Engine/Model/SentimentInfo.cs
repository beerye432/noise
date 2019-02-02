using System;
using System.Collections.Generic;
using System.Text;

namespace Noise.SentimentCollection.Engine
{
    /// <summary>
    /// This class contains information about the 
    /// results of running noise sentiment analysis 
    /// on a particular list of tokens.
    /// </summary>
    public class SentimentInfo
    {
        public SentimentInfo(int valence, int numTokens)
        {
            Valence = valence;
            NumTokens = numTokens;
            ValenceAverage = (float)valence / numTokens;
        }
        /// <summary>
        /// The sum of all valence values for tokens
        /// that appear in the AFINN-en-165 list
        /// </summary>
        public int Valence { get; set; }

        /// <summary>
        /// The number of tokens that appeared
        /// in AFINN-en-165.txt for a particular list
        /// of tokens
        /// </summary>
        public int NumTokens { get; set; }

        /// <summary>
        /// Valence total divided by number of identified tokens
        /// </summary>
        public float ValenceAverage { get; private set; }

        /// <summary>
        /// Set of tokens that were identified as positive
        /// </summary>
        public HashSet<string> PositiveTokens { get; set; }

        /// <summary>
        /// Set of tokens that were identified as negative
        /// </summary>
        public HashSet<string> NegativeTokens { get; set; }

        /// <summary>
        /// Set of tokens that were identified as neutral
        /// </summary>
        public HashSet<string> NeutralTokens { get; set; }

        /// <summary>
        /// Set of tokens that were identified as proper nouns
        /// </summary>
        public HashSet<string> ProperNounTokens { get; set; }
    }
}
