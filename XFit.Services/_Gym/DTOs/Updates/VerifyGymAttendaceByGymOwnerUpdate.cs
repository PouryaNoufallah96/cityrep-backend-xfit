using XFit.Utilities.Attributes;

namespace XFit.Services._Gym.DTOs.Updates
{
    public class VerifyGymAttendaceByGymOwnerUpdate
    {
        [StringInputValidation(maxLength: 150)] public string AttendanceReference { get; set; } 
    }
}
