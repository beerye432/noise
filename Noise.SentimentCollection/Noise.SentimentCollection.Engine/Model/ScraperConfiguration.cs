using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Noise.SentimentCollection.Engine
{
    /// <summary>
    /// This class represents options for the webscraper 
    /// on a per-domain basis. For example, it might contain 
    /// a site URL, how to navigate it's URL's to find articles, 
    /// and what HTML elemnts to look into for relevant text.
    /// </summary>
    [DataContract]
    public class RSSScraperConfiguration
    {
        [DataMember]
        public SentimentTopic Type { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string RSSURL { get; set; }

        [DataMember]
        public string LinkLocalName { get; set; }

        [DataMember]
        public List<string> RelevantTags { get; set; }
    }

    /// <summary>
    /// Enumeration describing methods for constructing URL's
    /// to relevant articles given a base URL. This will probably
    /// have to be determined by trial and error / by hand.
    /// </summary>
    public enum TraversalMethod
    {
        TraversalMethodOne = 0
    }
}
