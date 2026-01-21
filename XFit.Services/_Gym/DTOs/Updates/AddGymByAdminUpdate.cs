using System.Text.Json.Serialization;
using Xfit.Domain.Collections;
using Xfit.Domain.Common;

namespace XFit.Services._Gym.DTOs.Updates
{
    public class AddGymUpdate
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public GymLevel Level { get; set; }
        public decimal Price { get; set; } 
        public AddressInfoUpdate Address { get; set; } 
        public Contact Contact { get; set; } = null;
        public List<GymImage> Images { get; set; } = null;
        public List<GymTrendInfoUpdate> Trends { get; set; } = null;
        public List<string> FacilityIds { get; set; } = null;
    }

    public class AddressInfoUpdate
    {
        public string Province { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
        //public double Longitude { get; set; }
        //public double Latitude { get; set; }
        public GeoLocation GeoLocation { get; set; }
    }
     
 

    public class EditGymUpdate
    {
        public string GymId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public AddressInfoUpdate Address { get; set; }
        public Contact Contact { get; set; } = null;
        public List<GymImage> Images { get; set; } = null;
        public List<GymTrendInfoUpdate> Trends { get; set; } = null;
        public List<string> FacilityIds { get; set; } = null;
    }

    public class GymTrendInfoUpdate
    {
        public string GymTrendId { get; set; }
        public List<GymTrendWorkingHourUpdate> Men { get; set; } = null;
        public List<GymTrendWorkingHourUpdate> Women { get; set; } = null;
    }


    public class GymTrendWorkingHourUpdate
    {
        public DayOfWeek DayOfWeek { get; set; }
        public List<GymSessionUpdate> Sessions { get; set; }
    }

    public class GymSessionUpdate
    {
        public decimal Price { get; set; }
        public GymTimeType TimeType { get; set; }
        public long From { get; set; }
        public long To { get; set; }
        public int? Capacity { get; set; }
    }

    public class AddGymByAdminUpdate : AddGymUpdate
    {
        public string GymOwnerPublicKey { get; set; }
    }

    public class EditGymByAdminUpdate : EditGymUpdate
    {
        public string GymOwnerPublicKey { get; set; }
        public GymLevel Level { get; set; }
    }


}
