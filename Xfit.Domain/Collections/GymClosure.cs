using XFit.Utilities.Attributes;
using XFit.Utilities.MongoDatabase.Documents;

namespace Xfit.Domain.Collections
{
    [MonjoCollectionName("GymClosuers")]
    public class GymClosure : BaseDocument
    {
        public string GymClosureId { get; set; } = Guid.NewGuid().ToString("N");
        public string GymId { get; set; }
        public string GymOwnerPublicKey { get; set; }
        public DateTime ClosureDate { get; set; } 
        public DayOfWeek DayOfWeek { get; set; }
        public bool IsAllDay { get; set; }
        public long? From { get; set; }
        public long? To { get; set; }
        public string Reason { get; set; }
    }
}
