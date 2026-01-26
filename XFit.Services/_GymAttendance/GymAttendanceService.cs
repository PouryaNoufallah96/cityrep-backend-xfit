using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Sentry;
using Xfit.Domain.Collections;
using Xfit.Domain.Repositories;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Deposit;
using XFit.Services._Deposit.DTOs;
using XFit.Services._Gym;
using XFit.Services._Gym.DTOs.Updates;
using XFit.Services._GymAttendance.DTOs;
using XFit.Services._Wallet;
using XFit.Utilities.Constants;
using XFit.Utilities.Exceptions.Common;
using XFit.Utilities.MongoDatabase.Extensions;
using XFit.Utilities.MongoDatabase.Filter;
using XFit.Utilities.Services.Contracts;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._GymAttendance
{
    public class GymAttendanceService(IGymAttendanceRepository _gymAttendanceRepository, ILogger<GymAttendanceService> _logger,
        IGymService _gymService,
        IDepositService _depositService,
        IRandomService _randomService,
        IWalletService _walletService,
        IGymClosureRepository _gymClosureRepository,
        IDepositRepository _depositRepository) : IGymAttendanceService, IScopedDependency
    {

        #region Client side

        /// <summary>
        /// this method use for create attendance by client
        /// </summary>
        /// <param name="update"></param>
        /// <param name="whois"></param>
        /// <returns></returns>
        /// <exception cref="NotFoundException"></exception>
        /// <exception cref="BadRequestException"></exception>
        public async Task<CreateGymAttendanceByClientResult> CreateGymAttendanceByClientAsync(
        CreateGymAttendanceUpdate update,
        string whois)
        {
            var gym = await _gymService.GetOneGymForInternalUsageAsync(update.GymId);

            var trend = gym.Trends
                ?.FirstOrDefault(t => t.GymTrendId == update.GymTrendId)
                ?? throw new NotFoundException("رشته ی ورزشی در باشگاه یافت نشد");

            GymSession session = null;
            DayOfWeek? sessionDay = null;

            void FindSession(IEnumerable<GymTrendWorkingHour> days)
            {
                if (days == null || session != null) return;

                foreach (var day in days)
                {
                    var found = day.Sessions?
                        .FirstOrDefault(s => s.GymSessionId == update.GymSessionId);

                    if (found != null)
                    {
                        session = found;
                        sessionDay = day.DayOfWeek;
                        return;
                    }
                }
            }

            FindSession(trend.Men);
            FindSession(trend.Women);

            if (session == null || sessionDay == null)
                throw new NotFoundException("تایم ورزشی در باشگاه یافت نشد");


            if (session.TimeType == GymTimeType.Session &&
            session.Capacity.HasValue &&
            session.UsedCapacity >= session.Capacity.Value)
            {
                throw new BadRequestException("ظرفیت این جلسه تکمیل شده است");
            }

            var iranTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
            var iranNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, iranTimeZone);
            var today = iranNow.Date;
            var validDays = new[]
            {
                today.DayOfWeek,
                today.AddDays(1).DayOfWeek
            };

            if (!validDays.Contains(sessionDay.Value))
                throw new BadRequestException(
                    "امکان رزرو فقط برای جلسات امروز یا فردا وجود دارد");

            var isExists = await _gymAttendanceRepository.AsQueryable()
                .AnyAsync(x =>
                    x.ClientPublicKey == whois &&
                    x.GymId == gym.GymId &&
                    x.GymTrendId == update.GymTrendId &&
                    x.GymSessionId == update.GymSessionId &&
                    x.GymAttendanceState == GymAttendanceState.Reserved);

            if (isExists)
                throw new BadRequestException(
                    Utilities.Enums.ApiResultStatusCode.Duplicated,
                    "شما قبلا این جلسه را رزرو کرده اید");

            var attendancePrice = session.Price;
            var clientBalance = await GetClientBalanceAsync(whois);


            DateTime GetSessionExpireTime(DayOfWeek day, long toMinutes)
            {
                var baseDate = today;
                var diff = ((int)day - (int)baseDate.DayOfWeek + 7) % 7;
                return baseDate.AddDays(diff).AddMinutes(toMinutes);
            }

            DateTime GetSessionDate(DayOfWeek day)
            {
                var baseDate = today;

                var diff = ((int)day - (int)baseDate.DayOfWeek + 7) % 7;

                return baseDate.AddDays(diff);
            }

            var expireAt = GetSessionExpireTime(sessionDay.Value, session.To);



            var newAttendance = new GymAttendance
            {
                SessionDate = GetSessionDate(sessionDay.Value),
                ClientPublicKey = whois,
                ClinetFullName = CurrentRequestContext.FullName,

                GymId = gym.GymId,
                GymTitle = gym.Title,
                GymTrendId = update.GymTrendId,
                GymTrendTitle = trend.Title,
                GymOwnerPublicKey = gym.GymOwnerPublicKey,
                GymAddress = gym.Address?.Address,
                GymImageUrl = gym.Images?
                    .OrderBy(i => i.Order)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault(),

                GymTimeType = session.TimeType,
                GymSessionId = session.GymSessionId,
                SessionPrice = session.Price,

                GymStart = session.From,
                GymEnd = session.To,
                ClientStartTime = null,
                Level = gym.Level,

                GymAttendanceReference = _randomService
                    .GetSecureAlphaNumericString(10)
                    .ToUpper(),

                ExpirePaymentCode = expireAt,
                GymAttendanceState = GymAttendanceState.Reserved,
            };

            await CheckGymIsOpenForSessionAsync(
            gym.GymId,
            newAttendance.SessionDate,
            session.From,
            session.To
            );

            var remain = clientBalance - attendancePrice;

            if (attendancePrice > clientBalance)
            {
                newAttendance.GymAttendanceState = GymAttendanceState.Pending;
                var depositRef = await _depositService.CreateDepositAsync(new CreateDepositUpdate { Amount = remain }, newAttendance.ClientPublicKey);
                newAttendance.DepositReference = depositRef;
            }

            await _gymAttendanceRepository.InsertOneAsync(newAttendance);

            bool isMenSession = trend.Men != null && trend.Men.Any(d => d.Sessions?.Any(s => s.GymSessionId == session.GymSessionId) == true);
            await _gymService.IncreaseSessionAvailableCapacityAsync(gym, session, isMenSession);
            await _walletService.MakeWalletShouldUpdateAsync(whois);

            return new CreateGymAttendanceByClientResult
            {
                AttendanceReference = newAttendance.GymAttendanceReference,
                GatewayUrl = newAttendance.GymAttendanceState == GymAttendanceState.Pending ? "gatewayurl" : null,
                Remain = remain,
                State = newAttendance.GymAttendanceState
            };
        }



        private async Task CheckGymIsOpenForSessionAsync(
        string gymId,
        DateTime sessionDate,
        long sessionFrom,
        long sessionTo)
        {
            var closures = await _gymClosureRepository.AsQueryable()
                .Where(c =>
                    c.GymId == gymId &&
                    c.ClosureDate.Date == sessionDate.Date
                )
                .ToListAsync();

            foreach (var closure in closures)
            {
                if (closure.IsAllDay)
                    throw new BadRequestException("باشگاه در این تاریخ تعطیل می‌باشد");

                if (closure.From.HasValue && closure.To.HasValue)
                {
                    var overlap =
                        sessionFrom < closure.To.Value &&
                        sessionTo > closure.From.Value;

                    if (overlap)
                        throw new BadRequestException("باشگاه در این بازه زمانی تعطیل می‌باشد");
                }
            }
        }


        /// <summary>
        /// this method use for get attendance list for client
        /// </summary>
        /// <param name="update"></param>
        /// <param name="whois"></param>
        /// <returns></returns>
        public async Task<GetClientGymAttendanceListResult> GetClientGymAttendanceListAsync(
        GetClientGymAttendanceListUpdate update,
        string whois)
        {
            var result = new GetClientGymAttendanceListResult();

            var query = _gymAttendanceRepository.AsQueryable()
                .Where(x => x.ClientPublicKey == whois);

            if (update.States != null && update.States.Any())
                query = query.Where(x => update.States.Contains(x.GymAttendanceState));

            if (update.Levels != null && update.Levels.Any())
                query = query.Where(x => update.Levels.Contains(x.Level));

            if (update.From.HasValue)
                query = query.Where(x => x.CreatedMoment >= update.From.Value);

            if (update.To.HasValue)
                query = query.Where(x => x.CreatedMoment <= update.To.Value);

            if (!string.IsNullOrWhiteSpace(update.Search))
            {
                var search = update.Search.Trim();

                query = query.Where(x =>
                    x.GymTitle.Contains(search) ||
                    x.GymTrendTitle.Contains(search) ||
                    x.GymAttendanceReference.Contains(search)
                );
            }

            result.TotalCount = await query.CountAsync();
            if (result.TotalCount == 0)
                return result;

            var page = update.Pagination?.Page > 0 ? update.Pagination.Page : 1;
            var size = update.Pagination?.Size > 0 ? update.Pagination.Size : 25;
            var skip = (page - 1) * size;

            result.PageCount = (int)Math.Ceiling(result.TotalCount / (double)size);

            var data = await query
                .OrderByDescending(x => x.CreatedMoment)
                .Skip(skip)
                .Take(size)
                .ToListAsync();

            result.Data = data.Select(x => new GetClientGymAttendanceResult
            {
                GymAttendanceId = x.GymAttendanceId,
                GymAttendanceReference = x.GymAttendanceReference,

                GymId = x.GymId,
                GymTitle = x.GymTitle,
                GymTrendId = x.GymTrendId,
                GymTrendTitle = x.GymTrendTitle,

                Notes = x.Notes,
                Level = x.Level,
                SessionPrice = x.SessionPrice,

                ExpirePaymentCode = x.ExpirePaymentCode,
                GymAttendanceState = x.GymAttendanceState,

                GivenRate = x.GivenRate,
                GymImageUrl = x.GymImageUrl,
                GymAddress = x.GymAddress,
                CreatedMoment = x.CreatedMoment,
                ModifiedMoment = x.ModifiedMoment,
                GymSessionId = x.GymSessionId,
                ClientStartTime = x.ClientStartTime,
                GymEnd = x.GymEnd,
                GymStart = x.GymStart,
                GymOwnerPublicKey = x.GymOwnerPublicKey,
                GymTimeType = x.GymTimeType,
                SessionDate = x.SessionDate,

            }).ToList();

            return result;
        }


        /// <summary>
        /// use for expire attendances that their payment code time is finished
        /// </summary>
        /// <returns></returns>
        public async Task ExpireAttendanceAsync()
        {
            var filter = Builders<GymAttendance>.Filter.And(
                Builders<GymAttendance>.Filter.Eq(x => x.GymAttendanceState, GymAttendanceState.Reserved),
                Builders<GymAttendance>.Filter.Lte(x => x.ExpirePaymentCode, DateTime.UtcNow)
            );

            var update = Builders<GymAttendance>.Update
                .Set(x => x.GymAttendanceState, GymAttendanceState.Expired);

            var result = await _gymAttendanceRepository.UpdateManyAsync(filter, update);

            _logger.LogInformation($"Expired {result.ModifiedCount} attendances");
        }


        /// <summary>
        /// this method use for add or update rate to attendance by client
        /// </summary>
        /// <param name="update"></param>
        /// <param name="whois"></param>
        /// <returns></returns>
        /// <exception cref="NotFoundException"></exception>
        /// <exception cref="BadRequestException"></exception>
        public async Task<bool> UpsertRateToAttendanceAsync(AddRateUpdate update, string whois)
        {
            var attendance = await _gymAttendanceRepository.FindOneAsync(q =>
                q.GymAttendanceId == update.GymAttendanceId &&
                q.ClientPublicKey == whois)
                ?? throw new NotFoundException("جلسه‌ی مورد نظر یافت نشد");

            if (attendance.GymAttendanceState != GymAttendanceState.Used)
                throw new BadRequestException("امتیازدهی فقط برای جلسات استفاده شده امکان‌پذیر است");

            var rateDeadline = attendance.ExpirePaymentCode.Value.AddHours(24);

            if (DateTime.UtcNow > rateDeadline)
                throw new BadRequestException("مهلت امتیازدهی این جلسه به پایان رسیده است");

            attendance.GivenRate = update.GivenRate;
            attendance.ModifiedMoment = DateTime.UtcNow;

            await _gymAttendanceRepository.ReplaceOneAsync(attendance);

            await _gymService.SyncRateOfGymAsync(attendance.GymId);

            return true;
        }

        #endregion


        #region GymOwner

        /// <summary>
        /// this method use for verify attendance by gym owner
        /// </summary>
        /// <param name="update"></param>
        /// <param name="whois"></param>
        /// <returns></returns>
        /// <exception cref="NotFoundException"></exception>
        /// <exception cref="BadRequestException"></exception>
        public async Task<bool> VerifyGymAttendaceByGymOwnerAsync(VerifyGymAttendaceByGymOwnerUpdate update, string whois)
        {
            var attendance = await _gymAttendanceRepository.FindOneAsync(q => q.GymOwnerPublicKey == whois && q.GymAttendanceReference.ToLower() == update.AttendanceReference.ToLower()) ??
                 throw new NotFoundException("جلسه ی مورد نظر یافت نشد");

            if (attendance.GymAttendanceState == GymAttendanceState.Used)
                throw new BadRequestException("این جلسه از قبل استفاده شده است");

            if (attendance.GymAttendanceState == GymAttendanceState.Expired)
                throw new BadRequestException("این جلسه منقضی  شده و  قابل تأیید نیست");

            if (attendance.ExpirePaymentCode < DateTime.UtcNow)
                throw new BadRequestException("مهلت پرداخت این جلسه به پایان رسیده است");

            attendance.GymAttendanceState = GymAttendanceState.Used;

            var iranTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
            var nowIran = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, iranTimeZone);
            attendance.ClientStartTime = nowIran.Hour * 60 + nowIran.Minute;

            await _gymAttendanceRepository.ReplaceOneAsync(attendance);
            await _walletService.MakeWalletShouldUpdateAsync(attendance.GymOwnerPublicKey);
            return true;
        }


        /// <summary>
        /// this method use for get attendance list for gym owner
        /// </summary>
        /// <param name="update"></param>
        /// <param name="whois"></param>
        /// <returns></returns>
        public async Task<GetGymOwnerGymAttendanceListResult> GetGymOwnerGymAttendanceListAsync(
        GetGymOwnerGymAttendanceListUpdate update,
        string whois)
        {
            var result = new GetGymOwnerGymAttendanceListResult();

            var query = _gymAttendanceRepository.AsQueryable()
                .Where(x => x.GymOwnerPublicKey == whois).Where(q => q.GymAttendanceState == GymAttendanceState.Used);

            //if (update.States != null && update.States.Any())
            //    query = query.Where(x => update.States.Contains(x.GymAttendanceState));

            if (update.Levels != null && update.Levels.Any())
                query = query.Where(x => update.Levels.Contains(x.Level));

            if (update.From.HasValue)
                query = query.Where(x => x.CreatedMoment >= update.From.Value);

            if (update.To.HasValue)
                query = query.Where(x => x.CreatedMoment <= update.To.Value);

            if (!string.IsNullOrWhiteSpace(update.Search))
            {
                var search = update.Search.Trim();

                query = query.Where(x =>
                    x.GymTitle.Contains(search) ||
                    x.GymTrendTitle.Contains(search) ||
                    x.GymAttendanceReference.Contains(search)
                );
            }

            result.TotalCount = await query.CountAsync();
            if (result.TotalCount == 0)
                return result;

            var page = update.Pagination?.Page > 0 ? update.Pagination.Page : 1;
            var size = update.Pagination?.Size > 0 ? update.Pagination.Size : 25;
            var skip = (page - 1) * size;

            result.PageCount = (int)Math.Ceiling(result.TotalCount / (double)size);

            var data = await query
                .OrderByDescending(x => x.CreatedMoment)
                .Skip(skip)
                .Take(size)
                .ToListAsync();


            result.Data = data.Select(x => new GetGymOwnerGymAttendanceResult
            {
                GymAttendanceId = x.GymAttendanceId,
                GymAttendanceReference = x.GymAttendanceReference,

                GymId = x.GymId,
                GymTitle = x.GymTitle,
                GymTrendId = x.GymTrendId,
                GymTrendTitle = x.GymTrendTitle,
                GymOwnerPublicKey = x.GymOwnerPublicKey,

                Notes = x.Notes,
                Level = x.Level,
                SessionPrice = x.SessionPrice,

                ExpirePaymentCode = x.ExpirePaymentCode,
                GymAttendanceState = x.GymAttendanceState,
                ClientStartTime = x.ClientStartTime,
                ClinetFullName = x.ClinetFullName,
                GymEnd = x.GymEnd,
                GymSessionId = x.GymSessionId,
                GymStart = x.GymStart,
                GymTimeType = x.GymTimeType,
                GivenRate = x.GivenRate,
                GymImageUrl = x.GymImageUrl,
                GymAddress = x.GymAddress,
                CreatedMoment = x.CreatedMoment,
                ModifiedMoment = x.ModifiedMoment,
                SessionDate = x.SessionDate
            }).ToList();

            return result;
        }


        /// <summary>
        /// this method use for get client balance
        /// </summary>
        /// <param name="whois"></param>
        /// <returns></returns>
        private async Task<decimal> GetClientBalanceAsync(string whois)
        {
            var clientDeposits = await _depositRepository.AsQueryable()
                .Where(q => q.SourcePublicKey == whois && q.State == Xfit.Domain.Collections.DepositState.Done)
                .SumAsync(q => q.Amount);

            var clientAttendance = await _gymAttendanceRepository.AsQueryable()
                .Where(q => q.ClientPublicKey == whois && q.GymAttendanceState != GymAttendanceState.Pending).SumAsync(q => q.SessionPrice);


            var balance = clientDeposits - clientAttendance;
            return balance;
        }

        #endregion


        #region Admin 
        public async Task<MonjoFilteredResult<GetGymOwnerGymAttendanceResult>> GetAllForAdminAsync(MonjoQuery query)
        {
            try
            {
                query.WithBase<GetGymOwnerGymAttendanceResult>();

                var data = await _gymAttendanceRepository.AsQueryable()
                   .Apply(query.Where)
                   .Apply(query.Order)
                   .Select(x => new GetGymOwnerGymAttendanceResult
                   {
                       GymAttendanceId = x.GymAttendanceId,
                       GymAttendanceReference = x.GymAttendanceReference,

                       GymId = x.GymId,
                       GymTitle = x.GymTitle,
                       GymTrendId = x.GymTrendId,
                       GymTrendTitle = x.GymTrendTitle,
                       GymOwnerPublicKey = x.GymOwnerPublicKey,

                       Notes = x.Notes,
                       Level = x.Level,
                       SessionPrice = x.SessionPrice,

                       ExpirePaymentCode = x.ExpirePaymentCode,
                       GymAttendanceState = x.GymAttendanceState,
                       ClientStartTime = x.ClientStartTime,
                       GymTimeType = x.GymTimeType,
                       GymStart = x.GymStart,
                       GymEnd = x.GymEnd,
                       ClinetFullName = x.ClinetFullName,
                       GymSessionId = x.GymSessionId,
                       GivenRate = x.GivenRate,

                       CreatedMoment = x.CreatedMoment,
                       ModifiedMoment = x.ModifiedMoment,
                       GymAddress = x.GymAddress,
                       SessionDate = x.SessionDate,
                       GymImageUrl = x.GymImageUrl
                   })
                   .ExecuteAsync(query);

                return data;
            }
            catch (Exception)
            {
                throw new BaseException();
            }
        }

        #endregion



    }
}

//public async Task<string> CreateGymAttendanceAsync(CreateGymAttendanceUpdate update, string whois)
//{
//    var gym = await _gymService.GetOneGymForInternalUsageAsync(update.GymId);
//    var trend = gym.Trends.FirstOrDefault(q => q.GymTrendId == update.GymTrendId) ?? throw new NotFoundException("رشته ی ورزشی در باشگاه یافت نشد");
//    var attendencePrice = GetGymPrice(gym);
//    var clientBalance = await GetClientBalanceAsync(whois);


//    if (attendencePrice > clientBalance)
//        throw new BadRequestException(
//            "اعتبار کیف پول شما کافی نمی باشد",
//            additionalData: new
//            {
//                remain = attendencePrice - clientBalance
//            }
//        );

//    var newAttendance = new GymAttendance
//    {
//        ClientPublicKey = whois,
//        GymId = gym.GymId,
//        GymTitle = gym.Title,
//        GymTrendId = update.GymTrendId,
//        GymTrendTitle = trend.Title,
//        GymOwnerPublicKey = gym.GymOwnerPublicKey,
//        Level = gym.Level,
//        PaymentState = GymAttendanceState.Pending,
//        Price = attendencePrice,
//        GymAttendanceReference = _randomService.GetSecureAlphaNumericString(10).ToUpper(),
//        PaymentMoment = null,
//        ExpirePaymentCode = DateTime.UtcNow.AddMicroseconds(20),
//    };

//    await _gymAttendanceRepository.InsertOneAsync(newAttendance);

//    return newAttendance.GymAttendanceReference;
//}
