using Xfit.Domain.Common;
using XFit.Utilities.Attributes;
using XFit.Utilities.DTOs;

namespace XFit.Services._Gym.DTOs.Updates
{
    public class GymFilter : GymSimpleFilter
    {
        public GymNearestFilter? Nearest { get; set; } = null;
       
    }


    public class GymNearestFilter
    {
        [NumericInputValidation(isRequired: true, mustBeNonZero: true)] public double Latitude { get; set; }
        [NumericInputValidation(isRequired: true, mustBeNonZero: true)] public double Longitude { get; set; }
        [NumericInputValidation(isRequired: true, mustBeNonZero: true)] public int MaxDistanceMeters { get; set; } = 10000;
    }


}
 