using Xfit.Domain.Collections;
using Xfit.Domain.Common;
using XFit.Services._Common.DTOs;

namespace XFit.Services._GymAttendance.DTOs
{
    public class CreateGymAttendanceUpdate
    {
        public string GymId { get; set; }
        public string GymTrendId { get; set; }
        public string GymSessionId { get; set; }
    }


   

}
