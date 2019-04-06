using Newtonsoft.Json;
using Noise.SentimentCollection.Engine;
using System.IO;

namespace Noise.SentimentConsumption
{
    public class NoiseConfigurations
    {
        private static NoiseConfigurations instance;

        private NoiseConfigurations() { }

        private string m_PostgresConnectionString;
        public static string PostgresConnectionString => Instance.m_PostgresConnectionString;


        private void Init()
        {
            // Load DB configuration
            string jsonString = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "dbconfig.json"));
            DatabaseConfiguration dbConfig = JsonConvert.DeserializeObject<DatabaseConfiguration>(jsonString);
            m_PostgresConnectionString = $"Host={dbConfig.Host};Port={dbConfig.Port};Username={dbConfig.Username};Password={dbConfig.Password};Database={dbConfig.Database}";
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
