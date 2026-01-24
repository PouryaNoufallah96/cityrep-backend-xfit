using Xfit.Domain.Common;
using XFit.Utilities.Attributes;

namespace Xfit.Domain.Collections
{

    [MonjoCollectionName("Clients")]
    public class Client : CommonUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; } 
        public string FullName { get; set; }
        public Gender Gender { get; set; }
        public string Email { get; set; } = null;
        public ClientAddressInfo Address { get; set; } = null;
        public DateTime? BirthDay { get; set; } = null;

    } 

    public class ClientAddressInfo
    {
        public string Province { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
    }
}
