using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            List<string> tokens = new List<string> { "FirstToken", "Brian", "Brian", "Plymouth", "Campari", "Carpano", "Antica", "cocktail" };

            // Act: Transform list of tokens into proper noun dictionary
            Dictionary<string, int> properNounTokens = SentimentUtils.GetProperNouns(tokens);

            // Assert: Check to see if dictionary has what we expect

            // Shouldn't contain "FirstToken", first token is ignored
            Assert.IsFalse(properNounTokens.ContainsKey("FirstToken"));

            // Get a distinct list of capitalized tokens from the initial list
            List<string> expected = tokens.Where(s => char.IsUpper(s[0])).Distinct().ToList();

            // Number of keys in dictionary should be equal to # of distinct elements in initial list that have capital letters
            Assert.AreEqual(expected.Count, properNounTokens.Keys.Count);

            // Dictionary should have a key for all expected values
            expected.ForEach(s => Assert.IsTrue(properNounTokens.ContainsKey(s)));

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
    }
}
