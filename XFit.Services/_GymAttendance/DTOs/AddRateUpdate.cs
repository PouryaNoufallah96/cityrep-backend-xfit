using XFit.Utilities.Attributes;

namespace XFit.Services._GymAttendance.DTOs
{
    public class AddRateUpdate
    {
        [StringInputValidation(maxLength: 150)] public string GymAttendanceId { get; set; }
        [NumericInputValidation(isRequired: true, max: 5, min: 0)] public decimal GivenRate { get; set; }
    }
}
