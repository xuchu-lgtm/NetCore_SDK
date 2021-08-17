using System.Collections.Generic;

namespace Space.HttpClient.Extensions
{
    public abstract class RequestBase
    {
        public abstract string ApiName { get; }

        public abstract void PrepareParam(IDictionary<string, object> paramters);

        public virtual IReadOnlyDictionary<string, object> GetParams()
        {
            var paramters = new Dictionary<string, object>();
            var param = new Dictionary<string, object>();

            PrepareParam(paramters);

            foreach (var (key, value) in paramters)
            {
                if (string.IsNullOrEmpty(key) || value == null || string.IsNullOrEmpty(value.ToString()))
                    continue;
                if (!param.ContainsKey(key))
                {
                    param.Add(key, value);
                }
            }

            return param;
        }
    }
}
