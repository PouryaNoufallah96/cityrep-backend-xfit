using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Xfit.Domain.Collections;
using XFit.Services._Deposit;
using XFit.Services._Deposit.DTOs;
using XFit.Utilities.Api;
using XFit.Utilities.Attributes;
using XFit.Utilities.Filters;
using XFit.Utilities.MongoDatabase.Filter;
using XFit.Utilities.Permissions;

namespace XFit.Controllers.V1
{

    [ApiController]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DepositController(IDepositService _depositService) : ApiBaseController
    {

        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "برای شارژ اکانت کاربر", Tags = ["C-Deposit"])]
        [CustomRateLimit(message: "تعداد درخواست‌ های متوالی زیاد. لطفاً ۵ دقیقه دیگر دوباره امتحان کنید", periodSeconds: (1 * 60), maxAttemptsCount: 30, lockoutDurationMinutes: 5)]
        [Authorize]
        public async Task<string> CreateDepositAsync(CreateDepositUpdate update)
            => await _depositService.CreateDepositAsync(update, PublicKey); 


        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "برای وریفای کردن واریز", Tags = ["C-Deposit"])]
        [CustomRateLimit(message: "تعداد درخواست‌ های متوالی زیاد. لطفاً ۵ دقیقه دیگر دوباره امتحان کنید", periodSeconds: (1 * 60), maxAttemptsCount: 30, lockoutDurationMinutes: 5)]
        [Authorize]
        public async Task<DepositResult> VerifyDepositAsync([FromBody] VerifyDepositUpdate update)
            => await _depositService.VerifyDepositAsync(update);



        [HttpPost("[action]")]
        [SwaggerOperation(Summary = "برای مشاهده ی واریز ها توسط ادمین", Tags = ["A-Deposit"])]
        [CustomRateLimit(message: "تعداد درخواست‌ های متوالی زیاد. لطفاً ۵ دقیقه دیگر دوباره امتحان کنید", periodSeconds: (1 * 60), maxAttemptsCount: 50, lockoutDurationMinutes: 5)]
        [Authorize(Permissions.CreateUser)]
        public async Task<MonjoFilteredResult<Deposit>> GetAllDepositsAsync([FromBody] MonjoQuery monjoQuery)
          => await _depositService.GetAllDepositsAsync(monjoQuery);


    }
}
