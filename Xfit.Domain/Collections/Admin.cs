using Xfit.Domain.Common;
using XFit.Utilities.Attributes;
using XFit.Utilities.MongoDatabase.Documents;

namespace Xfit.Domain.Collections
{

    [MonjoCollectionName("Admins")]
    public class Admin : BaseDocument
    {
        public string PublicKey { get; set; } = Guid.NewGuid().ToString("N");
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
        public UserRole Role { get; set; }
        public required string UserName { get; set; }
        public string PasswordHash { get; set; }  
        public required UserStatus Status { get; set; }
        public List<DateTime> LoginDates { get; set; } = [];
        public List<string> Permissions { get; set; } = [];

    }
}
