using Microsoft.Extensions.Caching.Memory;
using XFit.Utilities.Exceptions.Common;
using XFit.Utilities.Services.Contracts;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Utilities.Services
{
    public class NonceService(IMemoryCache cache) : INonceService, ISingletonDependency
    {
        public bool TryUse(string nonce, TimeSpan ttl)
        {
            if (cache.TryGetValue(nonce, out _))
                return false;

            cache.Set(nonce, true, ttl);
            return true;
        }
    }
}
