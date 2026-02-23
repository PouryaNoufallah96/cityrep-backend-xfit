using Xfit.Domain.Common;
using XFit.Utilities.DTOs;

namespace XFit.Services._Gym.DTOs.Updates
{
    public class GymSessionsListUpdate
    {
        public Pagination Pagination { get; set; } = new Pagination();
        public List<Gender> Genders { get; set; } = [];
        public List<string> GymTrendIds { get; set; } = [];
        public List<DayOfWeek> Days { get; set; } = [];
        public List<GymSessionActivity> SessionActivity { get; set; } = [];
        public string Search { get; set; } = null;
    } 

    public enum GymSessionActivity { Active, Deactive }
}
