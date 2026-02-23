using Xfit.Domain.Collections;
using Xfit.Domain.Common;
using XFit.Utilities.Attributes;

namespace XFit.Services._Gym.DTOs.Updates
{
    public class EditGymImagesUpdate
    {
        [StringInputValidation(maxLength:50)]public string GymId { get; set; } 
        public List<GymImage> Images { get; set; }
    }

    public class EditGymCommonDataUpdate 
    {
        [StringInputValidation(maxLength:50)]public string GymId { get; set; }
        [StringInputValidation(maxLength:200)]public string Title { get; set; }
        [StringInputValidation(maxLength:11)]public string PhoneNumber { get; set; }
        public List<Gender> Genders { get; set; } = new List<Gender>();
        [StringInputValidation(maxLength:700)] public string AddressText { get; set; } 

    }
    public class EditGymGeoLocationUpdate
    {
        [StringInputValidation(maxLength:50)]public string GymId { get; set; }
        public GeoLocation GeoLocation { get; set; }
    }
}
