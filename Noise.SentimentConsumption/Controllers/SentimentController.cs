using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Noise.Shared;

namespace Noise.SentimentConsumption
{
    public class SentimentController : Controller
    {
        private ISentimentManager IOCSentimentManager;

        public SentimentController(ISentimentManager sentimentManager)
        {
            IOCSentimentManager = sentimentManager;
        }

        [HttpGet]
        public async Task<ObjectResult> GetLatestSentiment(SentimentTopic topic)
        {
            Dictionary<string, object> ret = await IOCSentimentManager.GetLatestSentiment(topic);
            return Ok(ret);
        }
    }
}
