using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Space.HttpClient.Extensions
{
    public abstract class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger _logger;

        protected GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public virtual void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, $"{nameof(OnException)}");
        }
    }
}
