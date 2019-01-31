using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Noise.SentimentCollection.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : Controller
    {
        // GET api/values
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var feedUrl = "http://rss.cnn.com/rss/cnn_us.rss";
            List<string> feedItems = new List<string>();
            var responseString = "";
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(feedUrl);
                var responseMessage = await client.GetAsync(feedUrl);
                responseString = await responseMessage.Content.ReadAsStringAsync();

                XDocument doc = XDocument.Parse(responseString);
                foreach (var item in doc.Root.Descendants().First(i => i.Name.LocalName == "channel").Elements().Where(i => i.Name.LocalName == "item"))
                    feedItems.Add(item.Elements().First(i => i.Name.LocalName == "guid").Value);
            }

            return Ok(feedItems);
        }

        // POST api/values
        [HttpPost]
        public void CreateSentiments([FromBody] string value)
        {
        }
    }
}
