using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using XFit.Utilities.MongoDatabase.Documents;

namespace Xfit.Domain.Common
{
    public class CommonUser : BaseDocument
    {
        public string PublicKey { get; set; } = Guid.NewGuid().ToString("N");
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
        public UserRole Role { get; set; } 
        public required string PhoneNumber { get; set; }
        [BsonDefaultValue(null)] public string VerificationCode { get; set; }
        [BsonDefaultValue(null)] public DateTime? VerificationCodeSentMoment { get; set; } 
        [BsonDefaultValue(0)] public int WrongVerificationTryCount { get; set; } = 0;
        public required UserStatus Status { get; set; } 
        public List<DateTime> LoginDates { get; set; } = []; 
        public List<string> Permissions { get; set; } = []; 
        [BsonDefaultValue(null)] public PhoneNumberUpdate PhoneNumberUpdate { get; set; } = null;

    }

    public enum UserStatus { Active, Ban, NotVerified }
    public enum UserRole { Client, GymOwner, Admin }

    public class PhoneNumberUpdate
    {
        public DateTime AddMoment { get; set; } = DateTime.UtcNow;
        public string PhoneNumber { get; set; }
        public string VerificationCode { get; set; }
    }

    public enum GymLevel
    {
        [Display(Name = "عادی")]
        Basic = 1,

        [Display(Name = "متوسط")]
        Intermediate = 2,

        [Display(Name = "پیشرفته")]
        Advanced = 3,

        [Display(Name = "حرفه‌ای")]
        Professional = 4
    }  

    public class Contact
    {
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public Dictionary<string, string> SocialMedia { get; set; } = null; 

    }
}
