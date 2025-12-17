using Microsoft.AspNetCore.Mvc;
using XFit.Services._Admin.DTOs;

namespace XFit.Services._Admin
{
    public interface IAdminService
    {
        Task<bool> ResetPasswordAsync(ResetPasswordUpdate update);
        Task<ActionResult> LoginAsync(LoginUpdate update);
    }
}
