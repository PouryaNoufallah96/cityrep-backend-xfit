using _CodeAssistant.Exceptions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Data;
using Xfit.Domain.Collections;
using Xfit.Domain.Common;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Admin.DTOs;
using XFit.Utilities.Constants;
using XFit.Utilities.Enums;
using XFit.Utilities.Exceptions;
using XFit.Utilities.Exceptions.Common;
using XFit.Utilities.Models.Storages;
using XFit.Utilities.Services.Contracts;
using XFit.Utilities.Utilities;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._Admin
{
    public class AdminService(IAdminRepository _adminRepository,
        IPasswordService _passwordService,
        ICaptchaService _captchaService,
        SecurityStampStorage securityStampStorage,
        ISmsService _smsService,
        IRandomService _randomService,
        IJwtService _jwtService,
        JwtServiceSettings _jwtSettings) : IAdminService , IScopedDependency
    {



        public async Task<bool> ResetPasswordAsync(ResetPasswordUpdate update)
        {
            var user = await _adminRepository.FindOneAsync(q => q.PublicKey == update.PublicKey);
            user.PasswordHash = _passwordService.Hash(update.Password);
            user.SecurityStamp = Guid.NewGuid().ToString("N");

            await _adminRepository.ReplaceOneAsync(user);

            securityStampStorage.UpdateSecurityStamp("Admin", user.PublicKey, user.SecurityStamp);

            return true;
        }

        public async Task<ActionResult> LoginAsync(LoginUpdate update)
        {
            //Validation
            ValidateClientInfo(update.ClientId, update.ClientSecret);


            var user = await _adminRepository.FindOneAsync(q => q.UserName.Equals(update.UserName, StringComparison.CurrentCultureIgnoreCase))
                ?? throw new NotFoundException(ExceptionMessages.UserNotFound);

            if (!_passwordService.Verify(update.Password, user?.PasswordHash))
            {
                throw new NotFoundException(ExceptionMessages.UserNotFound);
            }

            if (user.Status == UserStatus.Ban)
                throw new BadRequestException(ExceptionMessages.UserIsBan);

            user = AddLoginDateToUser(user);
            await _adminRepository.ReplaceOneAsync(user);

            return _jwtService.Authenticate(user.PublicKey, "Admin", "Admin", user.UserName, user.Permissions, user.SecurityStamp);
        }


        /// <summary>
        /// this method use for add login date to user login dates list
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="BaseException"></exception>
        private static Admin AddLoginDateToUser(Admin admin)
        {
            try
            {
                if (admin.LoginDates != null && admin.LoginDates.Count >= 1)
                {
                    admin.LoginDates.Add(DateTime.UtcNow);

                    var userLoginDates = admin.LoginDates.OrderByDescending(x => x).ToList();
                    if (userLoginDates.Count > 20)
                    {
                        var twentiethDate = userLoginDates[19];
                        userLoginDates.RemoveAll(q => q <= twentiethDate);
                        admin.LoginDates = userLoginDates;
                    }
                }
                else
                    admin.LoginDates = [DateTime.UtcNow];

                return admin;
            }
            catch (Exception ex)
            {
                throw new BaseException(ex.Message);
            }
        }

        /// <summary>
        /// use for validate client info
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <exception cref="BadRequestException"></exception>
        private void ValidateClientInfo(string clientId, string clientSecret)
        {
            if (!clientId.HasValue() ||
                !clientSecret.HasValue() ||
                !_jwtSettings.ClientInfo.ContainsKey(clientId.ToLower()) ||
                !_jwtSettings.ClientInfo[clientId.ToLower()].Equals(clientSecret, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(ApiResultStatusCode.OAuth.ToDisplay());
        }


    }
}
