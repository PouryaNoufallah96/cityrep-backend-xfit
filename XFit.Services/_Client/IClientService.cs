using XFit.Services._Client.DTOs.Results;
using XFit.Services._Client.DTOs.Updates;
using XFit.Services._Common;

namespace XFit.Services._Client
{
    public interface IClientService : ICommonAuthService
    {

        Task<ClientResult> UpsertProfileDataAsync(ClientProfileDataUpdate update, string publicKey);
        Task<ClientResult> GetClientDataAsync(string publicKey);

    }
}
