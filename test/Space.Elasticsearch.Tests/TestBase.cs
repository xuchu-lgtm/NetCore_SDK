using System;
using System.Collections.Generic;
using System.Text;
using Space.ElasticSearch.Abstractions;
using Space.ElasticSearch.Configuration;
using Space.ElasticSearch.Implementations;
using Xunit.Abstractions;

namespace Space.ElasticSearch.Tests
{
    public abstract class TestBase
    {
        protected IElasticSearchClient Client;

        protected ITestOutputHelper Output;

        protected TestBase(ITestOutputHelper output)
        {
            Output = output;
            Client = new ElasticSearchClient(new ElasticSearchConfigProvider(new ElasticSearchOptions()
            {
                Nodes = new List<ElasticSearchNode>()
                {
                    new ElasticSearchNode() {Host = "192.168.0.254", Port = 9200},
                    new ElasticSearchNode() {Host = "192.168.0.254", Port = 9201},
                }
            }));
        }
    }
}
