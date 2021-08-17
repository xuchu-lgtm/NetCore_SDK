using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Space
{
    public static class ServiceCollectionExtensions
    {
        public static ISpaceBuilder AddSpaceBuilder([NotNull] this IServiceCollection services, [NotNull] IConfiguration configuration)
        {
            //TODO 自己的基础类在这里注入

            return new SpaceBuilder(services, configuration);
        }
    }
}
