using Xfit.Domain.Common;
using XFit.Utilities.Attributes;
using XFit.Utilities.MongoDatabase.Documents;

namespace Xfit.Domain.Collections
{
    [MonjoCollectionName("GymAttendances")]
    public class GymAttendance : BaseDocument
    {
        public string GymAttendanceId { get; set; } = Guid.NewGuid().ToString("N");
        public string GymAttendanceReference { get; set; }
        public string ClientPublicKey { get; set; }
        public string ClinetFullName { get; set; }


        // destination 
        public string GymId { get; set; }
        public string GymTitle { get; set; }
        public string GymTrendId { get; set; }
        public string GymTrendTitle { get; set; }
        public string GymOwnerPublicKey { get; set; }
        public AddressInfo GymAddress { get; set; } 
        public string GymImageUrl { get; set; }
        public GymTimeType GymTimeType { get; set; }
        public string GymSessionId { get; set; }
        public decimal SessionPrice { get; set; }
        public long GymStart { get; set; }
        public long GymEnd { get; set; }
        public DateTime SessionDate { get; set; }
        public string DepositReference { get; set; } 

        public long? ClientStartTime { get; set; } = null;
        public string Notes { get; set; }
        public GymLevel Level { get; set; }
        public DateTime? ExpirePaymentCode { get; set; }
        public GymAttendanceState GymAttendanceState { get; set; }

        public decimal? GivenRate { get; set; } = null; 
    }

    public enum GymAttendanceState {Pending, Reserved, Used, Expired , Failed };
}
