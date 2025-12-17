using Xfit.Domain.Common;
using XFit.Utilities.Attributes;
using XFit.Utilities.MongoDatabase.Documents;

namespace Xfit.Domain.Collections
{
    [MonjoCollectionName("Deposits")]
    public class Deposit : BaseDocument
    {
        public string DepositId { get; set; } = Guid.NewGuid().ToString("N");
        public string DepositReference { get; set; }
        public decimal Amount { get; set; }
        public string UserPublicKey { get; set; }
        public UserRole Role { get; set; }
        public string UserProfileId { get; set; }
        public string UserFullName { get; set; }


        public DepositState State { get; set; }
        public List<string> Errors { get; set; }
    }

    public enum DepositState { Pending, Done, Cancel, Failed }
}
