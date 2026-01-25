using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Data;
using Xfit.Domain.Collections;
using Xfit.Domain.Common;
using Xfit.Domain.Repositories;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Client.DTOs.Results;
using XFit.Services._Client.DTOs.Updates;
using XFit.Services._Common.DTOs;
using XFit.Services._Wallet;
using XFit.Utilities.Constants;
using XFit.Utilities.Enums;
using XFit.Utilities.Exceptions;
using XFit.Utilities.Exceptions.Common;
using XFit.Utilities.Permissions;
using XFit.Utilities.Services.Contracts;
using XFit.Utilities.Utilities;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._Client
{
    public class ClientService(IClientRepository _clientRepository,
        ICaptchaService _captchaService,
        ISmsService _smsService,
        IRandomService _randomService,
        IJwtService _jwtService,
        IGymAttendanceRepository _gymAttendanceRepository,
        IWalletService _walletService,
        JwtServiceSettings _jwtSettings) : IClientService, IScopedDependency
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
                var (client, isNew) = await GetOrCreateClientAsync(update.PhoneNumber);

                if (isNew)
                {
                    // TODO: Send new Verification code with SMS
                    //await _smsService.SendVerificationMessageAsync(newUser.PhoneNumber, newUser.VerificationCode);
                    return true;
                }


                if (client.Status == UserStatus.Ban)
                    throw new BadRequestException(ExceptionMessages.UserIsBan);

                var now = DateTime.UtcNow;

                if (client.VerificationCodeSentMoment != null &&
                    client.VerificationCodeSentMoment.Value.AddSeconds(5) > now)
                {
                    throw new BadRequestException("لطفا چند دقیقه بعد تلاش کنید");
                }

                if (client.VerificationCodeSentMoment == null ||
                    client.VerificationCodeSentMoment.Value.AddMinutes(10) < now)
                {
                    //client.VerificationCode = _randomService.GetSecureNumericString(4);
                    client.VerificationCode = "1234";
                    client.VerificationCodeSentMoment = now;

                    await _clientRepository.ReplaceOneAsync(client);


                }

                // TODO:UnComment
                //await _smsService.SendVerificationMessageAsync(client.PhoneNumber, client.VerificationCode);
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

                var client = await _clientRepository.FindOneAsync(q => q.PhoneNumber == update.PhoneNumber)
                ?? throw new NotFoundException(ExceptionMessages.UserNotFound);

                if (client.Status == UserStatus.Ban)
                    throw new BadRequestException(ExceptionMessages.UserIsBan);

                if (client.WrongVerificationTryCount > 3)
                {
                    if (!update.CaptchaKey.HasValue() || !update.CaptchaCode.HasValue())
                        throw new BadRequestException("Captcha required!");
                    await _captchaService.ValidateCaptchaAsync(update.CaptchaKey, update.CaptchaCode);
                }

                if (client.VerificationCode == null || client.VerificationCodeSentMoment == null)
                    throw new BadRequestException("لطفا مجدد تلاش کنید");

                if (client.VerificationCodeSentMoment != null
                    && client.VerificationCodeSentMoment.Value.AddMinutes(10) < DateTime.UtcNow)
                    throw new ExpireVerificationException(ExceptionMessages.CodeExpired);

                if (client.VerificationCode != update.VerificationCode)
                {
                    throw new BadRequestException(ExceptionMessages.CodeIsWrong);
                }

                client.VerificationCode = null;
                client.WrongVerificationTryCount = 0;
                client.VerificationCodeSentMoment = null;

                if (client.Status == UserStatus.NotVerified)
                    client.Status = UserStatus.Active;
                client = AddLoginDateToUser(client);
                await _clientRepository.ReplaceOneAsync(client);
                await _walletService.InitWalletAsync(client.PublicKey, UserRole.Client);
                return _jwtService.Authenticate(client.PublicKey, "Client", client.PhoneNumber, client.FullName ?? "", client.Permissions, client.SecurityStamp, client.FullName.HasValue());
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

                var client = await GetOneClientForInternalUsageAsync(whoIs);

                if (client.Status == UserStatus.Ban)
                    throw new BadRequestException(ExceptionMessages.UserIsBan);

                return _jwtService.Authenticate(client.PublicKey, role, client.PhoneNumber, client.FullName ?? " ", client.Permissions, client.SecurityStamp, client.FullName.HasValue());
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
            var client = await GetOneClientForInternalUsageAsync(whois);
            var newPhoneNumber = update.PhoneNumber;

            if (client.PhoneNumber == newPhoneNumber)
                throw new BadRequestException("شماره تلفن جدید نباید با شماره فعلی یکسان باشد.");

            if (await _clientRepository.ExistsAsync(x => x.PhoneNumber == newPhoneNumber))
                throw new BadRequestException("این شماره قبلاً توسط کاربر دیگری استفاده شده است.");

            var verificationCode = _randomService.GetSecureNumericString(4);

            client.PhoneNumberUpdate = new PhoneNumberUpdate
            {
                PhoneNumber = newPhoneNumber,
                VerificationCode = verificationCode,
                AddMoment = DateTime.UtcNow
            };
            client.WrongVerificationTryCount = 0;

            await _clientRepository.ReplaceOneAsync(client);

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
            var client = await GetOneClientForInternalUsageAsync(whois);

            if (client.PhoneNumberUpdate == null)
                throw new BadRequestException("درخواستی برای تغییر شماره ثبت نشده است.");

            if (client.WrongVerificationTryCount > 3)
            {
                if (!update.CaptchaKey.HasValue() || !update.CaptchaCode.HasValue())
                    throw new BadRequestException("Captcha required!");
                await _captchaService.ValidateCaptchaAsync(update.CaptchaKey, update.CaptchaCode);
            }

            if ((DateTime.UtcNow - client.PhoneNumberUpdate.AddMoment).TotalMinutes > 10)
                throw new ExpireVerificationException(ExceptionMessages.CodeExpired);

            if (client.PhoneNumberUpdate.VerificationCode != update.VerificationCode)
            {
                client.WrongVerificationTryCount++;
                await _clientRepository.ReplaceOneAsync(client);
                throw new BadRequestException("کد تایید اشتباه است.");
            }
            if (await _clientRepository.ExistsAsync(q => q.PhoneNumber == client.PhoneNumberUpdate.PhoneNumber))
                throw new BadRequestException("این شماره هم‌اکنون توسط کاربر دیگری استفاده می‌شود.");

            client.PhoneNumber = client.PhoneNumberUpdate.PhoneNumber;
            client.PhoneNumberUpdate = null;
            client.WrongVerificationTryCount = 0;

            await _clientRepository.ReplaceOneAsync(client);
            return true;
        }


        /// <summary>
        /// use for get one client for internal usage
        /// </summary>
        /// <param name="whois"></param>
        /// <returns></returns>
        /// <exception cref="NotFoundException"></exception>
        public async Task<Client> GetOneClientForInternalUsageAsync(string whois)
        {
            var client = await _clientRepository.FindOneAsync(q => q.PublicKey == whois)
                ?? throw new NotFoundException(ExceptionMessages.UserNotFound);
            return client;
        }


        /// <summary>
        /// use for upsert profile data
        /// </summary>
        /// <param name="update"></param>
        /// <param name="whois"></param>
        /// <returns></returns>
        public async Task<ClientResult> UpsertProfileDataAsync(ClientProfileDataUpdate update, string whois)
        {
            var client = await GetOneClientForInternalUsageAsync(whois);

            var oldFirstName = client.FirstName;
            var oldLastName = client.LastName;

            client.FirstName = update.FirstName?.Trim();
            client.LastName = update.LastName?.Trim();
            client.FullName = client.FirstName + " " + client.LastName;
            client.BirthDay = update.BirthDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            client.Gender = update.Gender;

            client.Address = new ClientAddressInfo
            {
                Province = update.Provice,
                City = update.City,
                Address = update.Address
            };

            bool nameChanged = oldFirstName != client.FirstName || oldLastName != client.LastName;

            await _clientRepository.ReplaceOneAsync(client);

            if (nameChanged)
            {
                var filter = Builders<GymAttendance>.Filter.Eq(x => x.ClientPublicKey, whois);
                var updateDefinition = Builders<GymAttendance>.Update
                    .Set(x => x.ClinetFullName, client.FullName);

                await _gymAttendanceRepository.UpdateManyAsync(filter, updateDefinition);
            }

            return ConvertToResult(client);
        }


        /// <summary>
        /// use for get client data
        /// </summary>
        /// <param name="whois"></param>
        /// <returns></returns>
        public async Task<ClientResult> GetClientDataAsync(string whois)
        {
            var client = await GetOneClientForInternalUsageAsync(whois);
            return ConvertToResult(client);

        }


        /// <summary>
        /// this method use for add login date to user login dates list
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="BaseException"></exception>
        private static Client AddLoginDateToUser(Client client)
        {
            try
            {
                if (client.LoginDates != null && client.LoginDates.Count >= 1)
                {
                    client.LoginDates.Add(DateTime.UtcNow);

                    var userLoginDates = client.LoginDates.OrderByDescending(x => x).ToList();
                    if (userLoginDates.Count > 20)
                    {
                        var twentiethDate = userLoginDates[19];
                        userLoginDates.RemoveAll(q => q <= twentiethDate);
                        client.LoginDates = userLoginDates;
                    }
                }
                else
                    client.LoginDates = [DateTime.UtcNow];

                return client;
            }
            catch (Exception ex)
            {
                throw new BaseException(ex.Message);
            }
        }


        /// <summary>
        /// use for get or create client by phone number
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        private async Task<(Client, bool)> GetOrCreateClientAsync(string phoneNumber)
        {
            var client = await _clientRepository.FindOneAsync(q => q.PhoneNumber == phoneNumber);
            bool isNew = false;

            if (client == null)
            {
                var newClient = new Client
                {
                    PhoneNumber = phoneNumber,
                    Status = UserStatus.NotVerified,
                    Role = UserRole.Client,
                    Permissions = GetPermissionsOfRole(UserRole.Client).Select(p => p.Code).ToList(),
                    //user.VerificationCode = _randomService.GetSecureNumericString(4);
                    VerificationCode = "1234",
                    VerificationCodeSentMoment = DateTime.UtcNow
                };

                await _clientRepository.InsertOneAsync(newClient);
                isNew = true;
            }

            return (client, isNew);
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

        private ClientResult ConvertToResult(Client client)
        {
            return new ClientResult
            {
                Status = client.Status,
                LoginDates = client.LoginDates,
                Role = client.Role,
                PhoneNumber = client.PhoneNumber,
                FirstName = client.FirstName,
                LastName = client.LastName,
                BirthDay = client.BirthDay == null
                    ? null
                    : DateOnly.FromDateTime(client.BirthDay.Value),
                Gender = client.Gender,
                Address = client.Address,
                Email = client.Email,
                CreatedMoment = client.CreatedMoment,
                ModifiedMoment = client.ModifiedMoment,
            };
        }


    }
}
