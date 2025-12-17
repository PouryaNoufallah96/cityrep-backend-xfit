using MongoDB.Bson.Serialization.Attributes;
using Xfit.Domain.Common;
using XFit.Utilities.Attributes;
using XFit.Utilities.MongoDatabase.Documents;

namespace Xfit.Domain.Collections
{
    [MonjoCollectionName("Withdrawals")]
    public class Withdrawal : BaseDocument
    {
        public string WithdrawalId { get; set; } = Guid.NewGuid().ToString();

        public string PublicKey { get; set; } 
        public UserRole Role { get; set; }
        public string ProfileId { get; set; }
        public string OwnerInfo { get; set; } 


        public string Reference { get; set; }
        public decimal Amount { get; set; }
        public WithdrawalState State { get; set; }

        public string VerificationCode { get; set; }
        [BsonDefaultValue(null)] public DateTime? VerificationCodeExpireMoment { get; set; }
        [BsonDefaultValue(3)] public int VerificationCodeVerifyCount { get; set; }


        public List<string> Exceptions { get; set; }
        public DateTime? WithdrawalDoneMoment { get; set; }

    }

    public enum WithdrawalState { NotVerified, Pending, Done, Canceled, Rejected, Failed }
}
