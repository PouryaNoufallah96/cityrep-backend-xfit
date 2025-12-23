using XFit.Utilities.Attributes;
using XFit.Utilities.DTOs;

namespace XFit.Services._Gym.DTOs.Updates
{
    public class GymListUpdate
    {
        public Pagination Pagination { get; set; } 
    }


    public class GymIdUpdate
    {
       [StringInputValidation(maxLength:200)] public string GymId { get; set; }
    }
}
