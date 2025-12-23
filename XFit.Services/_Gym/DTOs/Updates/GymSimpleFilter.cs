using Xfit.Domain.Common;
using XFit.Utilities.DTOs;

namespace XFit.Services._Gym.DTOs.Updates
{
    public class GymSimpleFilter
    { 
        public Pagination Pagination { get; set; } = new();
        public List<Gender> Genders { get; set; } = [];
        public List<GymLevel> GymLevels { get; set; } = [];
        public List<string> GymTrendIds { get; set; } = [];
        public List<string> FacilityIds { get; set; } = [];
        public string Search { get; set; } = "";
    }
}
