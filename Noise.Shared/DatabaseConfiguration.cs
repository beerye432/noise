using System.Runtime.Serialization;

namespace Noise.SentimentCollection.Engine
{
    /// <summary>
    /// Represents data needed to connect to our database
    /// </summary>
  
    [DataContract]
    public class DatabaseConfiguration
    {
        [DataMember]
        public string Host { get; set; }

        [DataMember]
        public int Port { get; set; }

        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public string Password { get; set; }

        [DataMember]
        public string Database { get; set; }
    }
}
