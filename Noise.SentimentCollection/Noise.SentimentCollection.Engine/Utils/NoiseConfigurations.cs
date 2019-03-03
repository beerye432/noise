using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Noise.SentimentCollection.Engine
{
    public class NoiseConfigurations
    {
        private static NoiseConfigurations instance;

        private NoiseConfigurations() { }

        private string m_PostgresConnectionString;
        public static string PostgresConnectionString => Instance.m_PostgresConnectionString;

        private List<RSSScraperConfiguration> m_ScraperTopics;
        public static List<RSSScraperConfiguration> ScraperTopics => Instance.m_ScraperTopics;

        private List<DomainSettings> m_KnownDomains;
        public static List<DomainSettings> KnownDomains => Instance.m_KnownDomains;

        private Dictionary<string, int> m_Valences;
        public static Dictionary<string, int> Valences => Instance.m_Valences;

        private List<string> m_IgnoredProperNouns;
        public static List<string> IgnoredProperNouns => Instance.m_IgnoredProperNouns;

        private void Init()
        {
            // Load DB configuration
            string jsonString = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "dbconfig.json"));
            DatabaseConfiguration dbConfig = JsonConvert.DeserializeObject<DatabaseConfiguration>(jsonString);
            m_PostgresConnectionString = $"Host={dbConfig.Host};Port={dbConfig.Port};Username={dbConfig.Username};Password={dbConfig.Password};Database={dbConfig.Database}";

            // Load scraper configuration
            jsonString = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "scraperconfig.json"));
            m_ScraperTopics = JsonConvert.DeserializeObject<List<RSSScraperConfiguration>>(jsonString);

            // Load domain settings
            jsonString = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "domainsettings.json"));
            m_KnownDomains = JsonConvert.DeserializeObject<List<DomainSettings>>(jsonString);

            // Create valence dictionary for NLP
            m_Valences = ValenceDictionaryUtils.CreateValenceDictionary().Result;

            // Create list of proper nouns to ignore
            // TODO: Move this to a file
            m_IgnoredProperNouns = new List<string> { "i", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"};
        }

        public static NoiseConfigurations Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new NoiseConfigurations();
                    instance.Init();
                }

                return instance;
            }
        }
    }
}
