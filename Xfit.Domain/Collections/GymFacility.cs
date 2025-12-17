using XFit.Utilities.Attributes;
using XFit.Utilities.MongoDatabase.Documents;

namespace Xfit.Domain.Collections
{
    [MonjoCollectionName("GymFacilities")]
    public class GymFacility : BaseDocument
    {
        public string FacilityId { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; }
    }
}
