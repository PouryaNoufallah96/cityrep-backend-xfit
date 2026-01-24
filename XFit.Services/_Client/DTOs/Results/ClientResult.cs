using Xfit.Domain.Collections;
using Xfit.Domain.Common;
using XFit.Services._Common.DTOs;

namespace XFit.Services._Client.DTOs.Results
{
    public class ClientResult : CommonResult
    {
        public UserStatus Status { get; set; }
        public List<DateTime> LoginDates { get; set; } = [];
        public UserRole Role { get; set; }
        public string PhoneNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public Gender Gender { get; set; }
        public string Email { get; set; } = null;
        public ClientAddressInfo Address { get; set; } = null;
        public DateOnly? BirthDay { get; set; } = null;
    }
}
