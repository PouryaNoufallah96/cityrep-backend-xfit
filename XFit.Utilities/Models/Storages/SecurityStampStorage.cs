using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Utilities.Models.Storages
{
    public class SecurityStampStorage : Dictionary<string, string>, ISelfSingletonDependency
    {
        public void UpdateSecurityStamp(string userType, string publicKey, string newSecurityStamp)
        {
            var storageKey = GetStorageKey(userType, publicKey);

            this[storageKey] = newSecurityStamp;
        }

        public void RemoveSecurityStamp(string userType, string publicKey)
        {
            var storageKey = GetStorageKey(userType, publicKey);

            this.Remove(storageKey);
        }

        public string GetSecurityStamp(string userType, string publicKey)
        {
            var storageKey = GetStorageKey(userType, publicKey);
            TryGetValue(storageKey, out var stamp);
            return stamp;
        }

        private static string GetStorageKey(string userType, string publicKey) => $"SS:{userType}:{publicKey}";
    }
}
