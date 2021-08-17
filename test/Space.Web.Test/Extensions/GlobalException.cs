using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Space.HttpClient.Extensions;

namespace Space.Web.Test.Extensions
{
    public class GlobalException : GlobalExceptionFilter
    {
        public GlobalException(ILogger<GlobalExceptionFilter> logger) : base(logger)
        {
            
        }

        public override void OnException(ExceptionContext context)
        {
            base.OnException(context);
            
        }
    }

    public class TestOperationResult<T> : OperationResult<T> where T : ErrorMessage
    {
        


        //public override OperationResult<T> ResultModel(T result)
        //{
        //    return base.ResultModel(result);
        //}
    }
}
