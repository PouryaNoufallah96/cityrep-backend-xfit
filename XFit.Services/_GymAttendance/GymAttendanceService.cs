using MongoDB.Driver.Linq;
using Xfit.Domain.Collections;
using Xfit.Domain.Repositories;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Gym;
using XFit.Services._Gym.DTOs.Results;
using XFit.Services._Gym.DTOs.Settings;
using XFit.Services._Gym.DTOs.Updates;
using XFit.Services._GymAttendance.DTOs;
using XFit.Utilities.Exceptions.Common;
using XFit.Utilities.MongoDatabase.Extensions;
using XFit.Utilities.MongoDatabase.Filter;
using XFit.Utilities.Services.Contracts;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._GymAttendance
{
    public class GymAttendanceService(IGymAttendanceRepository _gymAttendanceRepository,
        GymLevelSettings _gymLevelSettings,
        IGymService _gymService,
        IRandomService _randomService,
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
        public async Task<string> CreateGymAttendanceByClientAsync(CreateGymAttendanceUpdate update, string whois)
        {
            var gym = await _gymService.GetOneGymForInternalUsageAsync(update.GymId);

            var trend = gym.Trends
                .FirstOrDefault(q => q.GymTrendId == update.GymTrendId)
                ?? throw new NotFoundException("رشته ی ورزشی در باشگاه یافت نشد");

            var attendancePrice = GetGymPrice(gym);
            var clientBalance = await GetClientBalanceAsync(whois);

            if (attendancePrice > clientBalance)
                throw new BadRequestException(
                    "اعتبار کیف پول شما کافی نمی باشد",
                    additionalData: new
                    {
                        remain = attendancePrice - clientBalance
                    });

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var pendingAttendance = await _gymAttendanceRepository.AsQueryable()
                .Where(x =>
                    x.ClientPublicKey == whois &&
                    x.GymId == gym.GymId &&
                    x.GymTrendId == update.GymTrendId &&
                    x.PaymentState == GymAttendanceState.Pending &&
                    x.CreatedMoment >= today &&
                    x.CreatedMoment < tomorrow
                )
                .FirstOrDefaultAsync();

            if (pendingAttendance != null)
            {
                pendingAttendance.Price = attendancePrice;
                pendingAttendance.ExpirePaymentCode = DateTime.UtcNow.AddMinutes(20);
                pendingAttendance.GymTrendTitle = trend.Title;
                pendingAttendance.GymTitle = gym.Title;
                pendingAttendance.Level = gym.Level;

                await _gymAttendanceRepository.ReplaceOneAsync(pendingAttendance);
                return pendingAttendance.GymAttendanceReference;
            }

            var newAttendance = new GymAttendance
            {
                ClientPublicKey = whois,
                GymId = gym.GymId,
                GymTitle = gym.Title,
                GymTrendId = update.GymTrendId,
                GymTrendTitle = trend.Title,
                GymOwnerPublicKey = gym.GymOwnerPublicKey,
                Level = gym.Level,
                PaymentState = GymAttendanceState.Pending,
                Price = attendancePrice,
                GymAttendanceReference = _randomService.GetSecureAlphaNumericString(10).ToUpper(),
                PaymentMoment = null,
                ExpirePaymentCode = DateTime.UtcNow.AddMinutes(20),
                CreatedMoment = DateTime.UtcNow,
                GymAddress = gym.Address.Address,
                GymImageUrl = gym.Images.Where(q => q.Order == 0).Select(q => q.ImageUrl).FirstOrDefault()
            };

            await _gymAttendanceRepository.InsertOneAsync(newAttendance);
            return newAttendance.GymAttendanceReference;
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
                query = query.Where(x => update.States.Contains(x.PaymentState));

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
                ClientPublicKey = x.ClientPublicKey,

                GymId = x.GymId,
                GymTitle = x.GymTitle,
                GymTrendId = x.GymTrendId,
                GymTrendTitle = x.GymTrendTitle,

                Notes = x.Notes,
                Level = x.Level,
                Price = x.Price,

                ExpirePaymentCode = x.ExpirePaymentCode,
                PaymentState = x.PaymentState,
                PaymentMoment = x.PaymentMoment,

                GivenRate = x.GivenRate,
                GymImageUrl = x.GymImageUrl,
                GymAddress = x.GymAddress,
                CreatedMoment = x.CreatedMoment,
                ModifiedMoment = x.ModifiedMoment
            }).ToList();

            return result;
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

            if (attendance.PaymentState != GymAttendanceState.Paid)
                throw new BadRequestException("امتیازدهی فقط برای جلسات پرداخت‌شده امکان‌پذیر است");

            if (!attendance.PaymentMoment.HasValue)
                throw new BadRequestException("زمان پرداخت جلسه مشخص نیست");

            var rateDeadline = attendance.PaymentMoment.Value.AddHours(24);

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
            var attendance = await _gymAttendanceRepository.FindOneAsync(q => q.GymOwnerPublicKey == whois && q.GymAttendanceId == update.GymAttendanceId) ??
                 throw new NotFoundException("جلسه ی مورد نظر یافت نشد");

            if (attendance.PaymentState == GymAttendanceState.Paid)
                throw new BadRequestException("این جلسه از قبل پرداخت شده است");

            if (attendance.PaymentState == GymAttendanceState.Fail)
                throw new BadRequestException("این جلسه ناموفق شده و قابل تأیید نیست");

            if (attendance.ExpirePaymentCode < DateTime.UtcNow)
                throw new BadRequestException("مهلت پرداخت این جلسه به پایان رسیده است");

            var clientBalance = await GetClientBalanceAsync(attendance.ClientPublicKey);

            if (attendance.Price > clientBalance)
            {
                attendance.PaymentState = GymAttendanceState.Fail;
                attendance.PaymentMoment = DateTime.UtcNow;
                await _gymAttendanceRepository.ReplaceOneAsync(attendance);

                throw new BadRequestException(
                        "اعتبار کیف پول کاربر کافی نمی باشد");
            }


            attendance.PaymentState = GymAttendanceState.Paid;
            attendance.PaymentMoment = DateTime.UtcNow;
            await _gymAttendanceRepository.ReplaceOneAsync(attendance);
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
                .Where(x => x.GymOwnerPublicKey == whois);

            if (update.States != null && update.States.Any())
                query = query.Where(x => update.States.Contains(x.PaymentState));

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
                Price = x.Price,

                ExpirePaymentCode = x.ExpirePaymentCode,
                PaymentState = x.PaymentState,
                PaymentMoment = x.PaymentMoment,

                GivenRate = x.GivenRate,
                GymImageUrl = x.GymImageUrl,
                GymAddress = x.GymAddress,
                CreatedMoment = x.CreatedMoment,
                ModifiedMoment = x.ModifiedMoment
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
                .Where(q => q.ClientPublicKey == whois && q.PaymentState == GymAttendanceState.Paid).SumAsync(q => q.Price);


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
                       Price = x.Price,

                       ExpirePaymentCode = x.ExpirePaymentCode,
                       PaymentState = x.PaymentState,
                       PaymentMoment = x.PaymentMoment,

                       GivenRate = x.GivenRate,
                       
                       CreatedMoment = x.CreatedMoment,
                       ModifiedMoment = x.ModifiedMoment,
                       GymAddress = x.GymAddress,
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


        private decimal GetGymPrice(Gym gym)
        {
            if (gym == null)
                throw new ArgumentNullException(nameof(gym));

            var levelData = _gymLevelSettings
                .FirstOrDefault(x => x.Level == gym.Level);

            if (levelData == null)
                throw new BadRequestException("سطح باشگاه یافت نشد");

            return levelData.Price;
        }

      
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