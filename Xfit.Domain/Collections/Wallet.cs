using Xfit.Domain.Common;
using XFit.Utilities.Attributes;
using XFit.Utilities.MongoDatabase.Documents;

namespace Xfit.Domain.Collections
{
    [MonjoCollectionName("Wallets")]
    public class Wallet : BaseDocument
    {
        public string WalletId { get; set; } = Guid.NewGuid().ToString("N");
        public string PublicKey { get; set; }
        public string UserInfo { get; set; }
        public UserRole Role { get; set; } 

        public decimal TotalBalance { get; set; }
        public decimal AvailableBalance { get; set; } 
        public decimal FrozenBalance { get; set; }

        public bool ShouldUpdate { get; set; } = false;

    }
}
