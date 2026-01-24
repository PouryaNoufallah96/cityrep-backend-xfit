using Xfit.Domain.Common;
using XFit.Utilities.Attributes;

namespace XFit.Services._Client.DTOs.Updates
{
    public class ClientProfileDataUpdate
    {
        [StringInputValidation(maxLength: 300)] public string FirstName { get; set; }
        [StringInputValidation(maxLength: 300)] public string LastName { get; set; }
        [RequiredInput] public DateOnly BirthDay { get; set; }
        [RequiredInput] public Gender Gender { get; set; }
        public string Provice { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
    }

    public class UpdateClientProfileUpdate : ClientProfileDataUpdate
    {
    }

}
