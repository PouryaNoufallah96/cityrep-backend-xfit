//using XFit.Services._GymClosure.DTOs;
//using XFit.Utilities.MongoDatabase.Filter;

//namespace XFit.Services._GymClosure
//{
//    public interface IGymClosureService
//    {

//        #region GymOwner Side
//        Task<GymClosureResult> CreateAsync(GymClosureCreateUpdate dto, string whois);
//        Task<GymClosureResult> UpdateAsync(GymClosureEditUpdate dto, string whois);
//        Task<GymClosureResult> DeleteAsync(RemoveGymClosureUpdate update, string whois);
//        Task<GymClosureResult> GetOneByIdAsync( GymClosureIdUpdate update);
//        Task<GymClosureListResult> GetListForGymOwnerAsync(GymClosureListUpdate update, string whois);

//        #endregion


//        #region Admin 

//        Task<GymClosureForAdminResult> CreateByAdminAsync(GymClosureCreateForAdminUpdate dto);
//        Task<GymClosureForAdminResult> UpdateByAdminAsync(GymClosureEditForAdminUpdate dto);
//        Task<GymClosureForAdminResult> DeleteByAdminAsync(RemoveGymClosureForAdminUpdate update);
//        Task<GymClosureForAdminResult> GetOneByIdByAdminAsync(GymClosureIdUpdate update);
//        Task<MonjoFilteredResult<GymClosureForAdminResult>> GetListForAdminAsync(MonjoQuery query); 


//        #endregion


//    }
//}
