namespace XFit.Services._GymClosure.DTOs
{

    public class GymClosureCreateForAdminUpdate : GymClosureCreateUpdate
    {
        public string GymOwnerPublicKey { get; set; }
    }
     

    public class GymClosureEditForAdminUpdate : GymClosureEditUpdate
    {
        public string GymOwnerPublicKey { get; set; }
    }
     
     
    public class RemoveGymClosureForAdminUpdate : RemoveGymClosureUpdate
    {
        public string GymOwnerPublicKey { get; set; }
    }



    public class GymClosureForAdminResult : GymClosureResult
    {
        public string GymOwnerPublicKey { get; set; }
    }








}
