using Microsoft.AspNetCore.Mvc;
using Xfit.Domain.Collections;
using Xfit.Domain.Common;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Common.DTOs;
using XFit.Services._GymOwner.DTOs.Results;
using XFit.Services._GymOwner.DTOs.Updates;
using XFit.Services._Wallet;
using XFit.Utilities.Constants;
using XFit.Utilities.Enums;
using XFit.Utilities.Exceptions;
using XFit.Utilities.Exceptions.Common;
using XFit.Utilities.Permissions;
using XFit.Utilities.Services.Contracts;
using XFit.Utilities.Utilities;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._GymOwner
{
    public class GymOwnerService(IGymOwnerRepository _gymOwnerRepository,
         ICaptchaService _captchaService,
        ISmsService _smsService,
        IWalletService _walletService,
        IRandomService _randomService,
        IJwtService _jwtService,
        JwtServiceSettings _jwtSettings) : IGymOwnerService, IScopedDependency
    {

        /// <summary>
        /// use for get verification code for user login or register
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        /// <exception cref="BaseException"></exception>
        public async Task<bool> GetVerificationCodeForAuthenticationAsync
            (GetVerificationCodeForAuthenticationUpdate update)
        {
            try
            {
                ValidateClientInfo(update.ClientId, update.ClientSecret);
                var (gymOwner, isNew) = await GetOrCreateGymOwnerAsync(update.PhoneNumber);

                if (isNew)
                {
                    // TODO: Send new Verification code with SMS
                    //await _smsService.SendVerificationMessageAsync(newUser.PhoneNumber, newUser.VerificationCode);
                    return true;
                }


                if (gymOwner.Status == UserStatus.Ban)
                    throw new BadRequestException(ExceptionMessages.UserIsBan);

                var now = DateTime.UtcNow;

                if (gymOwner.VerificationCodeSentMoment != null &&
                    gymOwner.VerificationCodeSentMoment.Value.AddSeconds(5) > now)
                {
                    throw new BadRequestException("لطفا چند دقیقه بعد تلاش کنید");
                }

                if (gymOwner.VerificationCodeSentMoment == null ||
                    gymOwner.VerificationCodeSentMoment.Value.AddMinutes(10) < now)
                {
                    //gymOwner.VerificationCode = _randomService.GetSecureNumericString(4);
                    gymOwner.VerificationCode = "1234";
                    gymOwner.VerificationCodeSentMoment = now;

                    await _gymOwnerRepository.ReplaceOneAsync(gymOwner);

                }

                // TODO:UnComment
                //await _smsService.SendVerificationMessageAsync(gymOwner.PhoneNumber, gymOwner.VerificationCode);
                return true;
            }
            catch (BadRequestException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BaseException(ex.Message);
            }
        }


        /// <summary>
        /// use for verify verification code for user
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        /// <exception cref="ExpireVerificationException"></exception>
        /// <exception cref="BadRequestException"></exception>
        /// <exception cref="BaseException"></exception>
        public async Task<ActionResult> VerifyAndLoginWithVerificationCodeAsync
            (VerifyAndLoginWithVerificationCodeUpdate update)
        {
            try
            {
                //Validation
                ValidateClientInfo(update.ClientId, update.ClientSecret);

                var gymOwner = await _gymOwnerRepository.FindOneAsync(q => q.PhoneNumber == update.PhoneNumber)
                ?? throw new NotFoundException(ExceptionMessages.UserNotFound);

                if (gymOwner.Status == UserStatus.Ban)
                    throw new BadRequestException(ExceptionMessages.UserIsBan);

                if (gymOwner.WrongVerificationTryCount > 3)
                {
                    if (!update.CaptchaKey.HasValue() || !update.CaptchaCode.HasValue())
                        throw new BadRequestException("Captcha required!");
                    await _captchaService.ValidateCaptchaAsync(update.CaptchaKey, update.CaptchaCode);
                }

                if (gymOwner.VerificationCode == null || gymOwner.VerificationCodeSentMoment == null)
                    throw new BadRequestException("لطفا مجدد تلاش کنید");

                if (gymOwner.VerificationCodeSentMoment != null
                    && gymOwner.VerificationCodeSentMoment.Value.AddMinutes(10) < DateTime.UtcNow)
                    throw new ExpireVerificationException(ExceptionMessages.CodeExpired);

                if (gymOwner.VerificationCode != update.VerificationCode)
                {
                    throw new BadRequestException(ExceptionMessages.CodeIsWrong);
                }

                gymOwner.VerificationCode = null;
                gymOwner.WrongVerificationTryCount = 0;
                gymOwner.VerificationCodeSentMoment = null;

                if (gymOwner.Status == UserStatus.NotVerified)
                    gymOwner.Status = UserStatus.Active;
                gymOwner = AddLoginDateToUser(gymOwner);
                await _gymOwnerRepository.ReplaceOneAsync(gymOwner);
                await _walletService.InitWalletAsync(gymOwner.PublicKey, UserRole.GymOwner);

                return _jwtService.Authenticate(gymOwner.PublicKey, "GymOwner", gymOwner.PhoneNumber,gymOwner.FullName, gymOwner.Permissions, gymOwner.SecurityStamp,gymOwner.FullName.HasValue());
            }
            catch (BadRequestException ex)
            {
                throw new BadRequestException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new BaseException(ex.Message);
            }

        }


        /// <summary>
        /// use for renew token of user
        /// </summary>
        /// <param name="token"></param>
        /// <param name="whoIs"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        /// <exception cref="BadRequestException"></exception>
        /// <exception cref="BaseException"></exception>
        public async Task<ActionResult> RenewTokenAsync(string token, string whoIs, string role)
        {
            try
            {
                var jwtToken = _jwtService.Validate(token);

                var gymOwner = await GetOneGymOwnerForInternalUsageAsync(whoIs);

                if (gymOwner.Status == UserStatus.Ban)
                    throw new BadRequestException(ExceptionMessages.UserIsBan);

                return _jwtService.Authenticate(gymOwner.PublicKey, role, gymOwner.PhoneNumber, gymOwner.FullName,gymOwner.Permissions, gymOwner.SecurityStamp,gymOwner.FullName.HasValue());
            }
            catch (BadRequestException ex)
            {
                throw new BadRequestException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new BaseException(ex.Message);
            }
        }


        /// <summary>
        /// use for request change phone number
        /// </summary>
        /// <param name="update"></param>
        /// <param name="whois"></param>
        /// <returns></returns>
        /// <exception cref="BadRequestException"></exception>
        public async Task<bool> RequestChangePhoneNumberAsync(ChangePhoneNumberUpdate update, string whois)
        {
            var gymOwner = await GetOneGymOwnerForInternalUsageAsync(whois);
            var newPhoneNumber = update.PhoneNumber;

            if (gymOwner.PhoneNumber == newPhoneNumber)
                throw new BadRequestException("شماره تلفن جدید نباید با شماره فعلی یکسان باشد.");

            if (await _gymOwnerRepository.ExistsAsync(x => x.PhoneNumber == newPhoneNumber))
                throw new BadRequestException("این شماره قبلاً توسط کاربر دیگری استفاده شده است.");

            var verificationCode = _randomService.GetSecureNumericString(4);

            gymOwner.PhoneNumberUpdate = new PhoneNumberUpdate
            {
                PhoneNumber = newPhoneNumber,
                VerificationCode = verificationCode,
                AddMoment = DateTime.UtcNow
            };
            gymOwner.WrongVerificationTryCount = 0;

            await _gymOwnerRepository.ReplaceOneAsync(gymOwner);

            //await _smsService.SendVerificationMessageAsync(newPhoneNumber, verificationCode);
            return true;
        }


        /// <summary>
        /// use for verify change phone number
        /// </summary>
        /// <param name="update"></param>
        /// <param name="whois"></param>
        /// <returns></returns>
        /// <exception cref="BadRequestException"></exception>
        /// <exception cref="ExpireVerificationException"></exception>
        public async Task<bool> VerifyChangePhoneNumberAsync(VerifyChangePhoneNumberUpdate update, string whois)
        {
            var gymOwner = await GetOneGymOwnerForInternalUsageAsync(whois);

            if (gymOwner.PhoneNumberUpdate == null)
                throw new BadRequestException("درخواستی برای تغییر شماره ثبت نشده است.");

            if (gymOwner.WrongVerificationTryCount > 3)
            {
                if (!update.CaptchaKey.HasValue() || !update.CaptchaCode.HasValue())
                    throw new BadRequestException("Captcha required!");
                await _captchaService.ValidateCaptchaAsync(update.CaptchaKey, update.CaptchaCode);
            }

            if ((DateTime.UtcNow - gymOwner.PhoneNumberUpdate.AddMoment).TotalMinutes > 10)
                throw new ExpireVerificationException(ExceptionMessages.CodeExpired);

            if (gymOwner.PhoneNumberUpdate.VerificationCode != update.VerificationCode)
            {
                gymOwner.WrongVerificationTryCount++;
                await _gymOwnerRepository.ReplaceOneAsync(gymOwner);
                throw new BadRequestException("کد تایید اشتباه است.");
            }
            if (await _gymOwnerRepository.ExistsAsync(q => q.PhoneNumber == gymOwner.PhoneNumberUpdate.PhoneNumber))
                throw new BadRequestException("این شماره هم‌اکنون توسط کاربر دیگری استفاده می‌شود.");

            gymOwner.PhoneNumber = gymOwner.PhoneNumberUpdate.PhoneNumber;
            gymOwner.PhoneNumberUpdate = null;
            gymOwner.WrongVerificationTryCount = 0;

            await _gymOwnerRepository.ReplaceOneAsync(gymOwner);
            return true;
        }


        /// <summary>
        /// use for get one gymOwner for internal usage
        /// </summary>
        /// <param name="whois"></param>
        /// <returns></returns>
        /// <exception cref="NotFoundException"></exception>
        public async Task<GymOwner> GetOneGymOwnerForInternalUsageAsync(string whois)
        {
            var gymOwner = await _gymOwnerRepository.FindOneAsync(q => q.PublicKey == whois)
                ?? throw new NotFoundException(ExceptionMessages.UserNotFound);
            return gymOwner;
        }


        /// <summary>
        /// use for upsert profile data of gym owner
        /// </summary>
        /// <param name="update"></param>
        /// <param name="whois"></param>
        /// <returns></returns>
        public async Task<GymOwnerResult> UpsertProfileDataAsync(GymOwnerProfileDataUpdate update, string whois)
        {
            var gymOwner = await GetOneGymOwnerForInternalUsageAsync(whois);

            gymOwner.FullName = update.FullName; 
            gymOwner.Address = update.Address;
            gymOwner.BirthDay = update.BirthDay == null ? null : update.BirthDay?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            gymOwner.Contact = update.Contact;
            gymOwner.NationalId = update.NationalId;
            gymOwner.Description = update.Description;

            await _gymOwnerRepository.ReplaceOneAsync(gymOwner);
            return ConvertToResult(gymOwner);
        }


        /// <summary>
        /// use for get gym owner data
        /// </summary>
        /// <param name="whois"></param>
        /// <returns></returns>
        public async Task<GymOwnerResult> GetGymOnwerDataAsync(string whois)
        {
            var gymOwner = await GetOneGymOwnerForInternalUsageAsync(whois);
            return ConvertToResult(gymOwner);
        }


        /// <summary>
        /// use for upsert identity documents of gym owner
        /// </summary>
        /// <param name="update"></param>
        /// <param name="whois"></param>
        /// <returns></returns>
        /// <exception cref="NotFoundException"></exception>
        public async Task<GymOwnerResult> UpsertIdentityDocumentsAsync( GymOwnerProfileIdentityDocumenDataUpdate update,string whois)
        {
            var gymOwner = await GetOneGymOwnerForInternalUsageAsync(whois);

            if (update?.IdentityDocumentUrls == null || !update.IdentityDocumentUrls.Any())
                return ConvertToResult(gymOwner);

            gymOwner.IdentityDocumentUrls ??= new List<IdentityDocumentInfo>();

            foreach (var doc in update.IdentityDocumentUrls)
            {
                // Create new document
                if (string.IsNullOrWhiteSpace(doc.IdentityDocumentInfoId))
                {
                    var newDocument = new IdentityDocumentInfo
                    {
                        Title = doc.Title,
                        Url = doc.Url,
                        Status = doc.Status,
                        CreatedMoment = DateTime.UtcNow
                    };

                    gymOwner.IdentityDocumentUrls.Add(newDocument);
                }
                else
                {
                    var existingDocument = gymOwner.IdentityDocumentUrls
                        .FirstOrDefault(x => x.IdentityDocumentInfoId == doc.IdentityDocumentInfoId);

                    if (existingDocument == null)
                        throw new NotFoundException("مدرک هویتی مورد نظر یافت نشد");

                    existingDocument.Title = doc.Title;
                    existingDocument.Url = doc.Url;
                    existingDocument.Status = doc.Status;
                    existingDocument.ModifiedMoment = DateTime.UtcNow;
                }
            }

            await _gymOwnerRepository.ReplaceOneAsync(gymOwner);
            return ConvertToResult(gymOwner);
        }





        /// <summary>
        /// this method use for add login date to user login dates list
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="BaseException"></exception>
        private static GymOwner AddLoginDateToUser(GymOwner gymOwner)
        {
            try
            {
                if (gymOwner.LoginDates != null && gymOwner.LoginDates.Count >= 1)
                {
                    gymOwner.LoginDates.Add(DateTime.UtcNow);

                    var userLoginDates = gymOwner.LoginDates.OrderByDescending(x => x).ToList();
                    if (userLoginDates.Count > 20)
                    {
                        var twentiethDate = userLoginDates[19];
                        userLoginDates.RemoveAll(q => q <= twentiethDate);
                        gymOwner.LoginDates = userLoginDates;
                    }
                }
                else
                    gymOwner.LoginDates = [DateTime.UtcNow];

                return gymOwner;
            }
            catch (Exception ex)
            {
                throw new BaseException(ex.Message);
            }
        }


        /// <summary>
        /// use for get or create gymOwner by phone number
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        private async Task<(GymOwner, bool)> GetOrCreateGymOwnerAsync(string phoneNumber)
        {
            var gymOwner = await _gymOwnerRepository.FindOneAsync(q => q.PhoneNumber == phoneNumber);
            bool isNew = false;

            if (gymOwner == null)
            {
                var newGymOwner = new GymOwner
                {
                    PhoneNumber = phoneNumber,
                    Status = UserStatus.NotVerified,
                    Role = UserRole.Client,
                    Permissions = GetPermissionsOfRole(UserRole.Client).Select(p => p.Code).ToList(),
                    //user.VerificationCode = _randomService.GetSecureNumericString(4);
                    VerificationCode = "1234",
                    VerificationCodeSentMoment = DateTime.UtcNow
                };

                await _gymOwnerRepository.InsertOneAsync(newGymOwner);
                isNew = true;
            }

            return (gymOwner, isNew);
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


        /// <summary>
        /// use for get permissions of role
        /// </summary>
        /// <param name="userRole"></param>
        /// <returns></returns>
        private static IEnumerable<PermissionMeta> GetPermissionsOfRole(UserRole userRole)
        => Permissions.PermissionsList.Where(p => p.Roles.Contains(userRole.ToString().ToLower()));


        private GymOwnerResult ConvertToResult(GymOwner gymOwner)
        {
            return new GymOwnerResult
            {
                FullName = gymOwner.FullName,
                PhoneNumber = gymOwner.PhoneNumber,
                Address = gymOwner.Address,
                BirthDay = gymOwner.BirthDay == null
                    ? null
                    : DateOnly.FromDateTime(gymOwner.BirthDay.Value),
                Contact = gymOwner.Contact,
                NationalId = gymOwner.NationalId,
                Description = gymOwner.Description,
                IdentityDocumentUrls = gymOwner.IdentityDocumentUrls,
                CreatedMoment = gymOwner.CreatedMoment,
                LoginDates = gymOwner.LoginDates,
                ModifiedMoment = gymOwner.ModifiedMoment,
                Role = gymOwner.Role,
                Status = gymOwner.Status,
            };
        }
    }
}
