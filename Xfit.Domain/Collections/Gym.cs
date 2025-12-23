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

        public string Title { get; set; }
        public string Description { get; set; }
        public GymLevel Level { get; set; }
        public string Slug { get; set; }
         
        public List<Gender> SupportedGender { get; set; }
        public GymState State { get; set; } 

        public AddressInfo Address { get; set; }
        public List<GymTrendWorkingHour> GymTotalWorkingHour { get; set; }

        public Contact Contact { get; set; } = null;
        public List<GymImage> Images { get; set; } = null;
        public List<GymTrendInfo> Trends { get; set; } = null;
        public List<GymFacilityRef> Facilities { get; set; } = null;

        public decimal Rate { get; set; }

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

        public GenderWorkingHours Men { get; set; } = new GenderWorkingHours();
        public GenderWorkingHours Women { get; set; } = new GenderWorkingHours();
    }

    public class GenderWorkingHours
    {
        public bool IsActive { get; set; } = true;
        public List<GymTrendWorkingHour> WorkingHours { get; set; } = null;
    }

    public class GymTrendWorkingHour
    {
        public DayOfWeek DayOfWeek { get; set; }
        public long? From { get; set; }
        public long? To { get; set; }
        public bool IsClosed { get; set; }
    }


}
