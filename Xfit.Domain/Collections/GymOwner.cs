using Xfit.Domain.Common;
using XFit.Utilities.Attributes;

namespace Xfit.Domain.Collections
{

    [MonjoCollectionName("GymOnwers")]
    public class GymOwner : CommonUser
    {
        public string FullName { get; set; }
        public DateTime? BirthDay { get; set; }
        public string NationalId { get; set; }
        public string Description { get; set; }
        public Contact Contact { get; set; }
        public AddressInfo Address { get; set; }
        public List<IdentityDocumentInfo> IdentityDocumentUrls { get; set; } = null;
    }

    public class IdentityDocumentInfo
    {
        public string IdentityDocumentInfoId { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime CreatedMoment { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedMoment { get; set; } = null;
        public IdentityDocumentStatus Status { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }


    public enum IdentityDocumentStatus
    {
        NotVerified, Verified, Rejected
    }

}
