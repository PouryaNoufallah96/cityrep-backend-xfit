using Xfit.Domain.Common;
using XFit.Utilities.Attributes;
using XFit.Utilities.DTOs;

namespace XFit.Services._Gym.DTOs.Updates
{
    public class GymFilter
    {
        public Pagination Pagination { get; set; } = new();
        public GymNearestFilter? Nearest { get; set; } = null;
        public List<Gender> Genders { get; set; } = [];
        public List<GymLevel> GymLevels { get; set; } = [];
        public List<string> GymTrendIds { get; set; } = [];
        public List<string> FacilityIds { get; set; } = [];
        public string Search { get; set; } = "";
    }


    public class GymNearestFilter
    {
        [NumericInputValidation(isRequired: true, mustBeNonZero: true)] public double Latitude { get; set; }
        [NumericInputValidation(isRequired: true, mustBeNonZero: true)] public double Longitude { get; set; }
        [NumericInputValidation(isRequired: true, mustBeNonZero: true)] public int MaxDistanceMeters { get; set; } = 10000;
    }


}
 