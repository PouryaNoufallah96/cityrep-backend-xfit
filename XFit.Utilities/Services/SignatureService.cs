using System.Security.Cryptography;
using XFit.Utilities.Services.Contracts;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Utilities.Services
{
    public class SignatureService : ISignatureService, IScopedDependency
    {
        public byte[] Sign(byte[] key, byte[] data)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(data);
        }
        public bool Verify(byte[] key, byte[] data, byte[] signature)
        {
            using HMACSHA256 hmac = new(key);
            byte[] computedHash = hmac.ComputeHash(data);
            return computedHash.SequenceEqual(signature);

        }
    }
}
