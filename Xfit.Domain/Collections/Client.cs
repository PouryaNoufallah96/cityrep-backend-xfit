using Xfit.Domain.Common;
using XFit.Utilities.Attributes;

namespace Xfit.Domain.Collections
{

    [MonjoCollectionName("Clients")]
    public class Client : CommonUser
    {
        public string FullName { get; set; }
        public Gender Gender { get; set; }
        public string Email { get; set; } = null;
        public AddressInfo Address { get; set; } = null;
        public DateOnly? BirthDay { get; set; } = null;

    } 
}
