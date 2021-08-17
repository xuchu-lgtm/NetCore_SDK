using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Space.Web.AspNetCore
{
    public abstract class SpaceStartup : IStartup
    {
        private readonly IConfiguration _configuration;

        protected SpaceStartup(IConfiguration configuration) => _configuration = configuration;

        public virtual IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            ConfigureSpaceServices(services.AddSpaceBuilder(_configuration));

            return GetServiceProvider(services);
        }

        protected virtual IServiceProvider GetServiceProvider(IServiceCollection services) => services.BuildServiceProvider();

        public virtual void Configure(IApplicationBuilder app)
        {
            //TODO 公共的加载处理
        }

        public abstract void ConfigureSpaceServices(ISpaceBuilder builder);
    }
}
