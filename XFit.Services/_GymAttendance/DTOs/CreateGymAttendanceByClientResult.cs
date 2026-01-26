using Xfit.Domain.Collections;

namespace XFit.Services._GymAttendance.DTOs
{
    public class CreateGymAttendanceByClientResult
    {
        public string AttendanceReference { get; set; }  
        public GymAttendanceState State { get; set; }
        public string GatewayUrl { get; set; }
        public decimal Remain { get; set; }
    }

}
