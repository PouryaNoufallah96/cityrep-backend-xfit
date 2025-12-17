using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Utilities.Models.Storages
{
    public class ApplicationPoolStorage : Dictionary<string, ApplicationPool>, ISelfSingletonDependency
    {
    }

    public class ApplicationPool
    {
        public string PreSharedKey { get; set; }
        public string MasterSignature { get; set; }
    }
}
