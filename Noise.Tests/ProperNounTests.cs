﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Noise.SentimentCollection.Engine;
using System.Collections.Generic;
using System.Linq;

namespace Noise.Tests
{
    [TestClass]
    public class ProperNounTests
    {
        /// <summary>
        /// Test basic functionality
        /// </summary>
        [TestMethod]
        public void ProperNounTests_BasicFunctionality()
        {
            // Arrange: Create list of tokens
            List<string> tokens = new List<string> { "FirstToken", "Brian", "break", "Brian", "break", "Plymouth", "break", "Campari", "break", "Carpano", "Antica", "cocktail" };
            List<string> expectedTokens = new List<string> { "Brian", "Plymouth", "Campari", "Carpano Antica" };

            // Act: Transform list of tokens into proper noun dictionary
            Dictionary<string, int> properNounTokens = SentimentUtils.GetProperNouns(tokens);

            // Assert: Check to see if dictionary has what we expect

            // Shouldn't contain "FirstToken", first token is ignored
            Assert.IsFalse(properNounTokens.ContainsKey("FirstToken"));

            // Number of keys in dictionary should be equal to # of distinct proper nouns, and compound proper nouns
            Assert.AreEqual(expectedTokens.Count, properNounTokens.Keys.Count);

            // Dictionary should have a key for all expected values
            expectedTokens.ForEach(s => Assert.IsTrue(properNounTokens.ContainsKey(s)));

            // "Brian" should have 2 as a value (it appears twice)
            Assert.AreEqual(2, properNounTokens["Brian"]);
        }

        /// <summary>
        /// Test that basic compound proper noun tokens are grouped together correctly
        /// </summary>
        [TestMethod]
        public void ProperNounTests_CompoundToken()
        {
            // Arrange: Create list of tokens that are made up one or more capitalized words
            List<string> tokens = new List<string> { "FirstToken", "Software", "Developer", "Brian", "break", "Russian", "Agent", "Donald", "Trump", "break", "New", "York" };

            // Act: Transform list of tokens into proper noun dictionary
            Dictionary<string, int> properNounTokens = SentimentUtils.GetProperNouns(tokens);

            // Assert: Check to see if dictionary has what we expect

            // Shouldn't contain "FirstToken", first token is ignored
            Assert.IsFalse(properNounTokens.ContainsKey("FirstToken"));

            // Should contain 3 tokens: "Software Developer Brian, Russian Agent Donald Trump, and New York", 
            Assert.AreEqual(3, properNounTokens.Keys.Count);

            // See if dictionary contains these complex proper nouns properly grouped into keys
            Assert.IsTrue(properNounTokens.ContainsKey("Software Developer Brian"));
            Assert.IsTrue(properNounTokens.ContainsKey("Russian Agent Donald Trump"));
            Assert.IsTrue(properNounTokens.ContainsKey("New York"));
        }

        /// <summary>
        /// Tests that capitalized nouns that come after the end of a sentence are skipped
        /// </summary>
        [TestMethod]
        public void ProperNounTests_StartOfSentence()
        {
            // Arrange: Create list of tokens that resemble crappy sentences
            List<string> tokens = new List<string> { "Yes", "that's", "me.", "That", "is", "my", "Negroni." };

            // Act: Transform list of tokens into proper noun dictionary
            Dictionary<string, int> properNounTokens = SentimentUtils.GetProperNouns(tokens);

            // Assert: Check to see if dictionary has what we expect

            // Shouldn't contain "Yes", first token is ignored
            Assert.IsFalse(properNounTokens.ContainsKey("Yes"));

            // Should contain 1 token: "Negroni", 
            Assert.AreEqual(1, properNounTokens.Keys.Count);

            // See if dictionary contains only legit proper noun: Negroni
            Assert.IsTrue(properNounTokens.ContainsKey("Negroni"));
        }

        /// <summary>
        /// Ignored proper nouns are designed to "break the chain" of the compound proper noun analysis
        /// </summary>
        [TestMethod]
        public void ProperNounTests_IgnoredProperNounWithinCompoundProperNoun()
        {
            // Arrange: Create list of tokens that resemble crappy sentences
            List<string> tokens = new List<string> { "FirstToken", "Brian", "Monday", "Clark", "that's", "not", "me." };

            // Act: Transform list of tokens into proper noun dictionary
            Dictionary<string, int> properNounTokens = SentimentUtils.GetProperNouns(tokens);

            // Assert: Check to see if dictionary has what we expect

            // Should contain 2 token: "Brian", "Clark", 
            Assert.AreEqual(2, properNounTokens.Keys.Count);

            // Shouldn't contain "Monday"
            Assert.IsFalse(properNounTokens.ContainsKey("Monday"));

            // Shouldn't contain "Brian Monday Clark"
            Assert.IsFalse(properNounTokens.ContainsKey("Brian Monday Clark"));

            // See if dictionary contains only legit proper noun: Brian, Clark
            Assert.IsTrue(properNounTokens.ContainsKey("Brian"));
            Assert.IsTrue(properNounTokens.ContainsKey("Clark"));
        }
    }
}
