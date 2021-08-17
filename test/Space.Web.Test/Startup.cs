using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Space.Cache;
using Space.Consul;
using Space.DnsClient;
using Space.Log4Net;
using Space.Redis.Configuration;
using Space.Web.AspNetCore;
using Space.Web.Test.Extensions;

namespace Space.Web.Test
{
    public class Startup : SpaceStartup
    {
        public Startup(IConfiguration configuration) : base(configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public override IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(x =>
            {
                x.Filters.Add<GlobalException>();

            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            return base.ConfigureServices(services);
        }

        public override void ConfigureSpaceServices(ISpaceBuilder builder)
        {
            var cacheOptions = new RedisCacheOptions();
            Configuration.Bind("RedisCacheOptions", cacheOptions);
            Configuration.GetSection("RedisCacheOptions");

            builder.AddRedisCache(x =>
            {
                x.InstanceName = "test";
                x.Configuration = cacheOptions.Configuration;
            }).AddConsul().AddDnsClient();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public override void Configure(IApplicationBuilder app)
        {
            base.Configure(app);

            app.UseLog4Net().UseConsul();

            app.UseMvc();

            app.UseEndpoint();
        }
    }
}
