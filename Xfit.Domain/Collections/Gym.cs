using System.ComponentModel.DataAnnotations;
using Xfit.Domain.Common;
using XFit.Utilities.Attributes;
using XFit.Utilities.MongoDatabase.Documents;

namespace Xfit.Domain.Collections
{
    [MonjoCollectionName("Gyms")]
    public class Gym : BaseDocument
    {
        public string GymId { get; set; } = Guid.NewGuid().ToString("N");
        public string GymOwnerPublicKey { get; set; }
        public DateTime GymOwnerLastUpdateMoment { get; set; } = DateTime.MinValue;
        public string Title { get; set; }
        public string Description { get; set; }
        public GymLevel Level { get; set; }
        public DateTime PriceTrackerDatetime { get; set; }  
        public string Slug { get; set; }
         
        public List<Gender> SupportedGender { get; set; }
        public List<GymTimeType> SupportedTimeType { get; set; } 
        public GymState State { get; set; } 

        public AddressInfo Address { get; set; }
        public List<GymTotalWorkingHour> GymTotalWorkingHour { get; set; }

        public Contact Contact { get; set; } = null;
        public List<GymImage> Images { get; set; } = null;
        public List<GymTrendInfo> Trends { get; set; } = null;
        public List<GymFacilityRef> Facilities { get; set; } = null;

        public decimal Rate { get; set; }
        public decimal RateCount { get; set; } 

    }

    
    public enum GymState
    {
        NotVerified,
        Active,
        Inactive,
        Ban
    }

    public class GymFacilityRef
    {
        public string FacilityId { get; set; }
        public string Title { get; set; }
    }

    public class GymImage
    {
        public string ImageUrl { get; set; }
        public int Order { get; set; }
    }

    public class GymTrendInfo
    {
        public string GymTrendId { get; set; }
        public string Title { get; set; }

        public List<GymTrendWorkingHour> Men { get; set; } = null;
        public List<GymTrendWorkingHour> Women { get; set; } = null;
    }

    public enum GymTimeType
    {
        FreeTime,
        Session
    }

    //public class GenderWorkingHours
    //{
    //    public bool IsActive { get; set; } = true;
    //    public List<GymTrendWorkingHour> WorkingHours { get; set; } = null;
    //}

    public class GymTrendWorkingHour
    {
        public DayOfWeek DayOfWeek { get; set; }       
        public List<GymSession> Sessions { get; set; }
    }

    //public class FreeTimeRange
    //{
    //    public long From { get; set; } 
    //    public long To { get; set; }
    //}

    public class GymSession
    {
        public string GymSessionId { get; set; } = Guid.NewGuid().ToString("N");
        public decimal Price { get; set; } 
        public GymTimeType TimeType { get; set; }
        public long From { get; set; }
        public long To { get; set; }
        public int? Capacity { get; set; }
    }


    public class GymTotalWorkingHour
    {
        public DayOfWeek DayOfWeek { get; set; }
        public long? From { get; set; }
        public long? To { get; set; }
        public bool IsClosed { get; set; }
    }

}
