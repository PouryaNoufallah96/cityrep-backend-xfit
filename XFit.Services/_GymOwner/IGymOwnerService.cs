using XFit.Services._Common;
using XFit.Services._GymOwner.DTOs.Results;
using XFit.Services._GymOwner.DTOs.Updates;

namespace XFit.Services._GymOwner
{
    public interface IGymOwnerService : ICommonAuthService
    {
        Task<GymOwnerResult> UpsertProfileDataAsync(GymOwnerProfileDataUpdate update, string publicKey);
        Task<GymOwnerResult> UpsertIdentityDocumentsAsync(GymOwnerProfileIdentityDocumenDataUpdate update, string whois);
        Task<GymOwnerResult> GetGymOnwerDataAsync(string publicKey);
    }
}
