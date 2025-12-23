namespace XFit.Utilities.Constants
{
    public static class CurrentRequestContext
    {
        private static readonly AsyncLocal<RequestUserInfo> _current = new();

        public static RequestUserInfo User
        {
            get => _current.Value;
            set => _current.Value = value;
        }


        public static string PublicKey => User?.PublicKey;
        public static string Role => User?.Role;
        public static string PhoneNumber => User?.PhoneNumber;
        public static string DisplayInfo => User?.DisplayInfo;
       
    }

    public class RequestUserInfo
    {
        public string PublicKey { get; set; } = "system";
        public string Role { get; set; } = "system";
        public string Type { get; set; } = "system";
        public IEnumerable<string> Permissions { get; set; } = [];
        public string PhoneNumber { get; set; }
        public string DisplayInfo => $"{Role} : {PhoneNumber}";
    }
}
