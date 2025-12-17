using XFit.Utilities.Attributes;

namespace XFit.Services._GymFacility.DTOs
{
    public class CreateGymFacilityUpdate
    {
        [StringInputValidation(maxLength: 250)] public string Title { get; set; }
    }

    public class EditGymFacilityUpdate
    {
        [StringInputValidation(maxLength: 250)] public string GymFacilityId { get; set; }
        [StringInputValidation(maxLength: 250)] public string Title { get; set; }
    }

    public class RemoveGymFacilityUpdate
    {
        [StringInputValidation(maxLength: 250)] public string GymFacilityId { get; set; }
    }

    public class GymFacilityIdUpdate
    {
        [StringInputValidation(maxLength: 250)] public string GymFacilityId { get; set; }
    }


    //public class GymFacilityResult 
    //{
    //    public string Title { get; set; }
    //    public string GymFacilityId { get; set; }

    //}




}
