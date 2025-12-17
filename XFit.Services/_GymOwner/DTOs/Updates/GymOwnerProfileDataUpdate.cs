using Xfit.Domain.Collections;
using Xfit.Domain.Common;

namespace XFit.Services._GymOwner.DTOs.Updates
{
    public class GymOwnerProfileDataUpdate
    {
        public string FullName { get; set; }
        public AddressInfo Address { get; set; } = null;
        public DateOnly? BirthDay { get; set; } = null;
        public string NationalId { get; set; }
        public string Description { get; set; }
        public Contact Contact { get; set; }
    }

    public class GymOwnerProfileIdentityDocumenDataUpdate
    { 
        public List<IdentityDocumentUpdate> IdentityDocumentUrls { get; set; } = null;
    }

    public class IdentityDocumentUpdate
    {
        public string IdentityDocumentInfoId { get; set; } = null;
        public IdentityDocumentStatus Status { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }

}
