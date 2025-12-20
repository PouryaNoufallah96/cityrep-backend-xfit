using Xfit.Domain.Collections;
using Xfit.Domain.Common;

namespace XFit.Services._Gym.DTOs.Updates
{
    public class AddGymUpdate
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public GymLevel Level { get; set; }
        public AddressInfo Address { get; set; }
        public Contact Contact { get; set; } = null;
        public List<GymImage> Images { get; set; } = null;
        public List<GymTrendInfoUpdate> Trends { get; set; } = null;
        public List<string> FacilityIds { get; set; } = null;

    }

    public class EditGymUpdate : AddGymUpdate
    {
        public string GymId { get; set; }
    }

    public class GymTrendInfoUpdate
    {
        public string GymTrendId { get; set; }
        public GenderWorkingHours Men { get; set; } = new GenderWorkingHours();
        public GenderWorkingHours Women { get; set; } = new GenderWorkingHours();
    }


    public class AddGymByAdminUpdate : AddGymUpdate
    {
        public string GymOwnerPublicKey { get; set; }
    }

    public class EditGymByAdminUpdate : EditGymUpdate
    {
        public string GymOwnerPublicKey { get; set; }

    }


}
