using System.Runtime.Serialization;

namespace Noise.SentimentCollection.Engine
{
    /// <summary>
    /// This class represents scraper utilities on a per domain basis.
    /// It will contain rules and information for parsing HTML
    /// of a certain domain's articeles
    /// </summary>
    [DataContract]
    public class DomainSettings
    {
        [DataMember]
        public string Domain { get; set; }

        [DataMember]
        public string RelevantElementType { get; set; }

        [DataMember]
        public string RelevantClassName { get; set; }
    }
}
