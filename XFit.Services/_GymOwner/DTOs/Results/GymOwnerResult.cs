using Xfit.Domain.Collections;
using Xfit.Domain.Common;
using XFit.Services._Common.DTOs;

namespace XFit.Services._GymOwner.DTOs.Results
{
    public class GymOwnerResult: CommonResult
    {
        public UserStatus Status { get; set; }
        public List<DateTime> LoginDates { get; set; } = [];
        public string PhoneNumber { get; set; }
        public UserRole Role { get; set; }
        public string FullName { get; set; }
        public AddressInfo Address { get; set; } = null;
        public DateOnly? BirthDay { get; set; } = null;
        public string NationalId { get; set; }
        public string Description { get; set; }
        public Contact Contact { get; set; }
        public List<IdentityDocumentInfo> IdentityDocumentUrls { get; set; } = null;
    }
}
