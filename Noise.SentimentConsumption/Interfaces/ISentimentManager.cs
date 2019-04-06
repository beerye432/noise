using Noise.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Noise.SentimentConsumption
{
    public interface ISentimentManager
    {
        Task<Dictionary<string, object>> GetLatestSentiment(SentimentTopic topic);
    }
}
