using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Space
{
    public interface ISpaceBuilder
    {
        IServiceCollection Services { get; }
        IConfiguration Configuration { get; }
    }

    internal class SpaceBuilder : ISpaceBuilder
    {
        public IServiceCollection Services { get; }
        public IConfiguration Configuration { get; }

        public SpaceBuilder(IServiceCollection services, IConfiguration configuration)
        {
            this.Services = services;
            this.Configuration = configuration;
        }
    }
}
