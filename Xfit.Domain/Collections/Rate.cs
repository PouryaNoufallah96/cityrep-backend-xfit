using XFit.Utilities.Attributes;
using XFit.Utilities.MongoDatabase.Documents;

namespace Xfit.Domain.Collections
{
    [MonjoCollectionName("Rates")]
    public class Rate : BaseDocument
    {
        public string RateId { get; set; } = Guid.NewGuid().ToString("N");
        public string UserPublicKey { get; set; }
        public string GymId { get; set; }
        public string GymAttendanceId { get; set; }
        public decimal GivenRate { get; set; } // 0-5

    }
}
