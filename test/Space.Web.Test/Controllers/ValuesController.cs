using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Space.DnsClient;
using Space.Redis.Abstractions;

namespace Space.Web.Test.Controllers
{
    public class TestModel
    {
        public string Name { get; set; } = null;
    }


    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly ILogger _logger;

        private readonly IRedisCache _cache;
        private readonly IDnsClientFactory _dnsClientFactory;

        public ValuesController(ILogger<ValuesController> logger
            , IRedisCache cache, IDnsClientFactory dnsClientFactory
            )
        {
            _logger = logger;
            _cache = cache;
            _dnsClientFactory = dnsClientFactory;
            //_cache = cache;
        }

        // GET api/values
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> Get()
        {
            var message = "测试消息";

            try
            {
                //var model = new TestModel();
                //if (model.Name.Equals("T"))
                //{

                //}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{message}", ex.Message);
            }



            //var result1 = await _cache.StringSetAsync("test_1", "test_value");
            //var resultValue = await _cache.StringGetAsync("test_1");

            _logger.LogError("test {message}", message + Guid.NewGuid());



            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<dynamic> Get(int id)
        {
            
            
            var address = await _dnsClientFactory.GetAddress();
            var ip = address.Address.ToString();
            var port = address.Port;



            await _cache.HashSetAsync("test1", "key1", "value11");
            await _cache.HashSetAsync("test1", "key1", "value12");
            await _cache.HashSetAsync("test1", "key1", "value13", true);
            //var test1Value = await _cache.HashGetAsync("test1", "key1");
            //await _cache.HashSetAsync("test2",
            //    new Dictionary<string, string>() { { "key1", "value1" }, { "key2", "value2" } });
            //var dicValue = await _cache.HashGetAllAsync("test2");
            //var testValue = await _cache.HashGetAsync("test2", "key2");

            await _cache.UpdateExpiryAsync("test1", TimeSpan.FromSeconds(60));

            await _cache.HashDeleteAsync("test2", "key1");


            return Ok($"{ip}:{port}");
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
