using System.Security.Cryptography;
using XFit.Utilities.Services.Contracts;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Utilities.Services
{
    public class RandomService : IRandomService, ISingletonDependency
    {
        public string GetSecureAlphaNumericString(int len)
        {
            return GetSecureRandomString(len, "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        }
        public string GetSecureNumericString(int len)
        {
            return GetSecureRandomString(len, "0123456789");
        }
        public string GetSecureRandomString(int len, string chars)
        {
            string result = "";

            byte[] bytes = new byte[len];

            using (var randomNumberGenerator = RandomNumberGenerator.Create())
            {
                randomNumberGenerator.GetBytes(bytes);

                for (int i = 0; i < len; i++)
                {
                    var b = bytes[i];
                    var index = (int)(b * (chars.Length - 1) / 255.0);
                    result += chars[index];
                }
            }

            return result;
        }
    }
}
