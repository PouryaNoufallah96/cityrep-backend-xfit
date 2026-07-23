using Xfit.Domain.Collections;
using Xfit.Domain.Common;
using XFit.Utilities.DTOs;

namespace XFit.Services._GymAttendance.DTOs
{
    public class GetGymOwnerGymAttendanceListUpdate
    {
        public Pagination Pagination { get; set; }

        public List<GymAttendanceState> States { get; set; } = [];
        public List<GymLevel> Levels { get; set; } = [];
        public List<string> GymTrendIds { get; set; } = [];

        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public DateTime? SessionDateFrom { get; set; }
        public DateTime? SessionDateTo { get; set; }
        public string Search { get; set; }
    }
}
