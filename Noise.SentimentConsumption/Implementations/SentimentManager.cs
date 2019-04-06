using Noise.Shared;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Noise.SentimentConsumption
{
    public class SentimentManager : ISentimentManager
    {
        public async Task<Dictionary<string, object>> GetLatestSentiment(SentimentTopic topic)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();

            using (NpgsqlConnection connection = new NpgsqlConnection(NoiseConfigurations.PostgresConnectionString))
            {
                await connection.OpenAsync();

                using (NpgsqlCommand command = new NpgsqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = @"
                        SELECT *
                        FROM sentiments
                        ORDER BY date DESC
                        LIMIT 1";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if(await reader.ReadAsync())
                        {
                            ret["topic"] = topic;
                            ret["valence"] = reader.GetFieldValueAsync<double>(2).Result;
                            ret["date"] = reader.GetFieldValueAsync<DateTime>(1).Result;
                        }
                    }
                }
            }

            return ret;
        }
    }
}