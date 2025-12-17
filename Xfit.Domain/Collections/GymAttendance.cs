using Xfit.Domain.Common;
using XFit.Utilities.Attributes;
using XFit.Utilities.MongoDatabase.Documents;

namespace Xfit.Domain.Collections
{
    [MonjoCollectionName("GymAttendances")]
    public class GymAttendance : BaseDocument
    {
        public string GymAttendanceId { get; set; } = Guid.NewGuid().ToString("N");

        //source
        public string UserProfileId { get; set; }
        public string UserPublicKey { get; set; }

        // destination 
        public string GymId { get; set; }
        public string GymTitle { get; set; }  
        public string GymTrendId { get; set; }        
        public string GymTrendTitle { get; set; }
        public string GymOwnerPublicKey { get; set; } 

        public string Notes { get; set; }
        public GymLevel Level { get; set; }  
        public decimal Price { get; set; }

        public DateTime? ExpirePaymentCode { get; set; }
        public GymAttendance PaymentState { get; set; }
        
    }

    public enum GymAttendanceState { Pending, Paid, Fail };
    }
