using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver.Linq;
using System.Security.Cryptography.X509Certificates;
using Xfit.Domain.Collections;
using Xfit.Domain.Common;
using Xfit.Domain.Repositories;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Gym.DTOs.Results;
using XFit.Services._Gym.DTOs.Settings;
using XFit.Services._Gym.DTOs.Updates;
using XFit.Services._GymClosure.DTOs;
using XFit.Utilities.Exceptions.Common;
using XFit.Utilities.MongoDatabase.Extensions;
using XFit.Utilities.MongoDatabase.Filter;
using XFit.Utilities.Services.Contracts;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._Gym
{
    public class GymService(IGymRepository _gymRepository,
        IGymTrendRepository _gymTrendRepository,
        IRandomService randomService,
        GymLevelSettings _gymLevelSettings,
        ILogger<GymService> _logger,
        IGymAttendanceRepository _gymAttendanceRepository,
        IGymClosureRepository _gymClosureRepository,
        IGymFacilityRepository _gymFacilityRepository) : IGymService, IScopedDependency
    {

        #region Gym Owner Side

        /// <summary>
        /// this method use for adding new gym by gym owner
        /// </summary>
        /// <param name="update"></param>
        /// <param name="whois"></param>
        /// <returns></returns>
        public async Task<GymResult> AddGymAsync(AddGymUpdate update, string whois)
        {

            var newGym = new Gym
            {
                Title = update.Title.Trim(),
                Description = update.Description.Trim(),
                Level = update.Level,
                Address = CreateAddressInfo(update.Address),
                Contact = update.Contact,
                Images = update.Images,
                State = GymState.NotVerified,
                Slug = CreateSlug(update.Title),
                GymOwnerPublicKey = whois,
                PriceTrackerDatetime = DateTime.UtcNow,
            };

            await GetFacilitiesAsync(newGym, update.FacilityIds);

            await HandleGymTrends(newGym, update.Trends);

            await _gymRepository.InsertOneAsync(newGym);
            return ConvertToResult(newGym);
        }


        /// <summary>
        /// for create address info for gym
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        private AddressInfo CreateAddressInfo(AddressInfoUpdate update)
        {
            var address = new AddressInfo
            {
                Address = update.Address,
                Province = update.Province,
                City = update.City,
                PostalCode = update.PostalCode,
                GeoLocation = update.GeoLocation,
                Location = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                     new GeoJson2DGeographicCoordinates(update.GeoLocation.Longitude, update.GeoLocation.Latitude)
                     )
            };
            return address;
        }


        /// <summary>
        /// handle gym trends and working hours
        /// </summary>
        /// <param name="gym"></param>
        /// <param name="trends"></param>
        /// <returns></returns>
        /// <exception cref="BadRequestException"></exception>
        private async Task HandleGymTrends(Gym gym, List<GymTrendInfoUpdate> trends)
        {
            if (trends == null || trends.Count == 0)
            {
                gym.Trends = new List<GymTrendInfo>();
                gym.SupportedGender = new List<Gender>();
                gym.SupportedTimeType = new List<GymTimeType>();
                gym.GymTotalWorkingHour = InitTotalWeek();
                return;
            }

            trends = trends
                .Where(t => !string.IsNullOrWhiteSpace(t.GymTrendId))
                .DistinctBy(t => t.GymTrendId)
                .ToList();

            var trendsData = await GetGymTrendsAsync(trends.Select(t => t.GymTrendId).ToList());

            var result = new List<GymTrendInfo>();

            foreach (var trendUpdate in trends)
            {

                var td = trendsData
                    .FirstOrDefault(t => t.GymTrendId == trendUpdate.GymTrendId) ?? throw new BadRequestException($"رشته ورزشی {trendUpdate.GymTrendId} نامعتبر است");

                var trend = new GymTrendInfo
                {
                    GymTrendId = trendUpdate.GymTrendId,
                    Title = td.Title,
                    TrendIconUrl = td.IconUrl,
                    Men = BuildGenderWorkingHours(trendUpdate.Men, gym.Level),
                    Women = BuildGenderWorkingHours(trendUpdate.Women, gym.Level)
                };

                result.Add(trend);
            }

            gym.Trends = result;
            gym.SupportedGender = CalculateSupportedGenderForGym(gym);
            gym.SupportedTimeType = CalculateSupportedGymTimeTypeForGym(gym);
            gym.GymTotalWorkingHour = CalculateGymTotalTimeForGym(gym);
            gym.WeekPrices = CalculateGymWeekTimeForGym(gym);
        }


        /// <summary>
        /// for build gender working hours
        /// </summary>
        /// <param name="input"></param>
        /// <param name="gymLevel"></param>
        /// <returns></returns>
        private List<GymTrendWorkingHour> BuildGenderWorkingHours(
        List<GymTrendWorkingHourUpdate> input,
        GymLevel gymLevel)
        {
            var result = InitWeek();

            if (input == null || input.Count == 0)
                return result;

            foreach (var day in result)
            {
                var source = input.FirstOrDefault(x => x.DayOfWeek == day.DayOfWeek);
                if (source == null || source.Sessions == null)
                    continue;

                day.Sessions = BuildAndValidateSessions(source.Sessions, gymLevel);
            }

            return result;
        }


        /// <summary>
        /// use for build and validate sessions
        /// </summary>
        /// <param name="sessions"></param>
        /// <param name="gymLevel"></param>
        /// <returns></returns>
        private List<GymSession> BuildAndValidateSessions(
        List<GymSessionUpdate> sessions,
        GymLevel gymLevel)
        {
            if (sessions == null || sessions.Count == 0)
                return null;

            var result = new List<GymSession>();

            foreach (var s in sessions)
            {
                var session = new GymSession
                {
                    GymSessionId = Guid.NewGuid().ToString("N"),
                    Price = s.Price,
                    TimeType = s.TimeType,
                    From = s.From,
                    To = s.To,
                    Capacity = s.Capacity,
                    UsedCapacity = s.TimeType == GymTimeType.FreeTime ? null : 0
                };

                ValidateSessionPrice(session.Price, gymLevel);
                ValidateSessionByType(session);

                result.Add(session);
            }

            ValidateSessionOverlap(result);

            return result;
        }


        /// <summary>
        /// use for check time overlap in sessions in one trend day
        /// </summary>
        /// <param name="sessions"></param>
        /// <exception cref="BadRequestException"></exception>
        private void ValidateSessionOverlap(List<GymSession> sessions)
        {
            var ordered = sessions.OrderBy(x => x.From).ToList();

            for (int i = 0; i < ordered.Count - 1; i++)
            {
                if (ordered[i].To > ordered[i + 1].From)
                    throw new BadRequestException("در یک روز، سشن‌های یک رشته نباید همپوشانی زمانی داشته باشند");
            }
        }


        /// <summary>
        /// use for validate session by type
        /// </summary>
        /// <param name="session"></param>
        /// <exception cref="BadRequestException"></exception>
        private void ValidateSessionByType(GymSession session)
        {
            if (session.TimeType == GymTimeType.FreeTime)
            {
                if (session.From >= session.To)
                    throw new BadRequestException("زمان شروع باید کمتر از زمان پایان باشد");

                if (session.Capacity != null)
                    throw new BadRequestException("FreeTime نباید ظرفیت داشته باشد");
            }

            if (session.TimeType == GymTimeType.Session)
            {
                ValidateSessionTime(session);

                if (session.Capacity == null || session.Capacity <= 0)
                    throw new BadRequestException(" سانس باید ظرفیت معتبر داشته باشد");
            }
        }


        /// <summary>
        /// use for get available durations 
        /// </summary>
        private static readonly HashSet<long> AllowedSessionDurations =
        new()
        {
            60,    // 1 hour
            90,    // 1.5 hour
            120,   // 2 hour
            180    // 3 hour
        };


        /// <summary>
        /// use for validate session time
        /// </summary>
        /// <param name="session"></param>
        /// <exception cref="BadRequestException"></exception>
        private void ValidateSessionTime(GymSession session)
        {
            if (session.From >= session.To)
                throw new BadRequestException("زمان شروع باید کمتر از زمان پایان باشد");

            var duration = session.To - session.From;

            if (!AllowedSessionDurations.Contains(duration))
                throw new BadRequestException("مدت زمان هر سشن فقط می‌تواند 1، 1.5، 2 یا 3 ساعت باشد");
        }


        /// <summary>
        /// use for check session price based on gym level
        /// </summary>
        /// <param name="price"></param>
        /// <param name="gymLevel"></param>
        /// <exception cref="NotFoundException"></exception>
        /// <exception cref="BadRequestException"></exception>
        private void ValidateSessionPrice(decimal price, GymLevel gymLevel)
        {
            var gymSetting = _gymLevelSettings.FirstOrDefault(q => q.Level == gymLevel)
                ?? throw new NotFoundException("Gym Level not found!");

            if (price < gymSetting.FromPrice || price > gymSetting.ToPrice)
                throw new BadRequestException("قیمت تعیین شده خارج از سطح باشگاه می‌باشد");
        }


        /// <summary>
        /// use for calculate supported gender for gym
        /// </summary>
        /// <param name="gym"></param>
        /// <returns></returns>
        private List<Gender> CalculateSupportedGenderForGym(Gym gym)
        {
            var result = new List<Gender>();

            if (gym.Trends == null || gym.Trends.Count == 0)
                return result;

            var hasMen = gym.Trends.Any(t =>
                t.Men != null &&
                t.Men.Any(d => d.Sessions != null && d.Sessions.Count > 0));

            if (hasMen)
                result.Add(Gender.Male);

            var hasWomen = gym.Trends.Any(t =>
                t.Women != null &&
                t.Women.Any(d => d.Sessions != null && d.Sessions.Count > 0));

            if (hasWomen)
                result.Add(Gender.Female);

            return result;
        }


        /// <summary>
        /// use for calculate supported gym time type for gym
        /// </summary>
        /// <param name="gym"></param>
        /// <returns></returns>
        private List<GymTimeType> CalculateSupportedGymTimeTypeForGym(Gym gym)
        {
            var result = new List<GymTimeType>();

            if (gym.Trends == null || gym.Trends.Count == 0)
                return result;

            var allSessions = gym.Trends
                .SelectMany(t => (t.Men ?? new List<GymTrendWorkingHour>())
                    .Concat(t.Women ?? new List<GymTrendWorkingHour>()))
                .Where(d => d.Sessions != null)
                .SelectMany(d => d.Sessions)
                .ToList();

            if (allSessions.Any(s => s.TimeType == GymTimeType.FreeTime))
                result.Add(GymTimeType.FreeTime);

            if (allSessions.Any(s => s.TimeType == GymTimeType.Session))
                result.Add(GymTimeType.Session);

            return result;
        }


        /// <summary>
        /// use for calculate gym total time for gym
        /// </summary>
        /// <param name="gym"></param>
        /// <returns></returns>
        private List<GymTotalWorkingHour> CalculateGymTotalTimeForGym(Gym gym)
        {
            var result = InitTotalWeek();

            if (gym.Trends == null || gym.Trends.Count == 0)
                return result;

            foreach (var day in result)
            {
                var daySessions = gym.Trends
                    .SelectMany(t => (t.Men ?? new List<GymTrendWorkingHour>())
                        .Concat(t.Women ?? new List<GymTrendWorkingHour>()))
                    .Where(d => d.DayOfWeek == day.DayOfWeek && d.Sessions != null)
                    .SelectMany(d => d.Sessions)
                    .ToList();

                if (!daySessions.Any())
                {
                    day.IsClosed = true;
                    day.From = null;
                    day.To = null;
                    continue;
                }

                day.IsClosed = false;
                day.From = daySessions.Min(s => s.From);
                day.To = daySessions.Max(s => s.To);
            }

            return result;
        }


        /// <summary>
        /// use for calculate gym week time for gym
        /// </summary>
        /// <param name="gym"></param>
        /// <returns></returns>
        private List<WeekPriceDetail> CalculateGymWeekTimeForGym(Gym gym)
        {
            var result = new List<WeekPriceDetail>();

            if (gym.Trends == null || !gym.Trends.Any())
                return result;

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                var prices = new List<decimal>();

                foreach (var trend in gym.Trends)
                {
                    // Men
                    if (trend.Men != null)
                    {
                        var menDay = trend.Men.FirstOrDefault(x => x.DayOfWeek == day);
                        if (menDay?.Sessions != null)
                        {
                            prices.AddRange(menDay.Sessions.Select(s => s.Price));
                        }
                    }

                    // Women
                    if (trend.Women != null)
                    {
                        var womenDay = trend.Women.FirstOrDefault(x => x.DayOfWeek == day);
                        if (womenDay?.Sessions != null)
                        {
                            prices.AddRange(womenDay.Sessions.Select(s => s.Price));
                        }
                    }
                }

                if (!prices.Any())
                    continue;

                result.Add(new WeekPriceDetail
                {
                    DayOfWeek = day,
                    MinPrice = prices.Min(),
                    MaxPrice = prices.Max()
                });
            }

            return result;
        }





        /// <summary>
        ///  this method use for init week working hours
        /// </summary>
        /// <returns></returns>
        private List<GymTrendWorkingHour> InitWeek()
        {
            var result = new List<GymTrendWorkingHour>();

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                result.Add(new GymTrendWorkingHour
                {
                    DayOfWeek = day,
                    Sessions = null
                });
            }

            return result;
        }

        /// <summary>
        /// use for init total week working hours
        /// </summary>
        /// <returns></returns>
        private List<GymTotalWorkingHour> InitTotalWeek()
        {
            var result = new List<GymTotalWorkingHour>();

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                result.Add(new GymTotalWorkingHour
                {
                    DayOfWeek = day,
                    From = null,
                    To = null,
                    IsClosed = true
                });
            }

            return result;
        }


        /// <summary>
        /// this method use to get facility details
        /// </summary>
        /// <param name="facilityIds"></param>
        /// <returns></returns>
        private async Task GetFacilitiesAsync(Gym gym, List<string> facilityIds)
        {
            try
            {
                if (facilityIds == null || facilityIds.Count == 0)
                {
                    gym.Facilities = new List<GymFacilityRef>();
                    return;
                }

                var uniqueIds = facilityIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct()
                    .ToList();

                var facilities = await _gymFacilityRepository
                    .Find(Builders<GymFacility>.Filter.In(f => f.FacilityId, uniqueIds))
                    .ToListAsync();

                var result = facilities.Select(f => new GymFacilityRef
                {
                    FacilityId = f.FacilityId,
                    Title = f.Title
                }).ToList();

                gym.Facilities = result;
            }
            catch (Exception e)
            {
                throw new BaseException("Error occurred in facilities", e);
            }
        }


        /// <summary>
        /// this method use to get gym trend details
        /// </summary>
        /// <param name="gymTrendIds"></param>
        /// <returns></returns>
        private async Task<List<GymTrend>> GetGymTrendsAsync(List<string> gymTrendIds)
        {
            var gymTrends = await _gymTrendRepository
                .Find(Builders<GymTrend>.Filter.In(gt => gt.GymTrendId, gymTrendIds))
                .ToListAsync();

            return gymTrends;
        }


        /// <summary>
        /// this method use edit gym
        /// </summary>
        /// <param name="update"></param>
        /// <param name="whois"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<GymResult> EditGymAsync(EditGymUpdate update, string whois)
        {
            var gym = await _gymRepository.AsQueryable()
                .Where(x => x.GymId == update.GymId && x.GymOwnerPublicKey == whois)
                .FirstOrDefaultAsync();

            if (gym == null)
                throw new BadRequestException("Gym not found");

            await UpdateGymBaseInfoAsync(gym, update);

            await UpdateGymFacilities(gym, update.FacilityIds);

            gym.State = GymState.NotVerified;
            gym.ModifiedMoment = DateTime.UtcNow;

            await _gymRepository.ReplaceOneAsync(gym);

            return ConvertToResult(gym);
        }


        /// <summary>
        /// use for update gym base info
        /// </summary>
        /// <param name="gym"></param>
        /// <param name="update"></param>
        private async Task UpdateGymBaseInfoAsync(Gym gym, EditGymUpdate update)
        {
            bool titleChanged = false;
            bool imagesChanged = false;

            if (!string.IsNullOrWhiteSpace(update.Title) &&
                gym.Title != update.Title.Trim())
            {
                gym.Title = update.Title.Trim();
                gym.Slug = CreateSlug(gym.Title);
                titleChanged = true;
            }

            gym.Description = update.Description?.Trim();
            gym.Address = CreateAddressInfo(update.Address);

            if (update.Contact != null)
                gym.Contact = update.Contact;

            if (update.Images != null && !update.Images.SequenceEqual(gym.Images))
            {
                gym.Images = update.Images;
                imagesChanged = true;
            }

            await _gymRepository.ReplaceOneAsync(gym);

            if (titleChanged || imagesChanged)
            {
                var filter = Builders<GymAttendance>.Filter.Eq(x => x.GymId, gym.GymId);
                var updateDef = Builders<GymAttendance>.Update.Combine();

                if (titleChanged)
                    updateDef = updateDef.Set(x => x.GymTitle, gym.Title);

                if (imagesChanged)
                {
                    var mainImage = gym.Images?
                        .OrderBy(i => i.Order)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault();
                    updateDef = updateDef.Set(x => x.GymImageUrl, mainImage);
                }

                if (updateDef != null)
                    await _gymAttendanceRepository.UpdateManyAsync(filter, updateDef);
            }
        }




        /// <summary>
        /// use for update gym facilities
        /// </summary>
        /// <param name="gym"></param>
        /// <param name="facilityIds"></param>
        /// <returns></returns>
        private async Task UpdateGymFacilities(Gym gym, List<string> facilityIds)
        {
            if (facilityIds == null)
            {
                gym.Facilities = [];
                return;
            }

            if (facilityIds.Any())
                await GetFacilitiesAsync(gym, facilityIds);
            else
                gym.Facilities = [];
        }


        /// <summary>
        /// use for upsert gym trends
        /// </summary>
        /// <param name="update"></param>
        /// <param name="whois"></param>
        /// <returns></returns>
        /// <exception cref="BadRequestException"></exception>
        public async Task<GymResult> UpsertGymTrendsAsync(
         UpsertGymTrendsUpdate update,
         string whois)
        {
            var gym = await _gymRepository.AsQueryable()
                .Where(x => x.GymId == update.GymId &&
                            x.GymOwnerPublicKey == whois)
                .FirstOrDefaultAsync();

            if (gym.GymOwnerLastUpdateMoment.AddDays(7) > DateTime.UtcNow)
                throw new BadRequestException("شما فقط هر هفته یکبار می‌توانید تغییرات رشته‌های ورزشی را اعمال کنید");

            if (gym == null)
                throw new BadRequestException("Gym not found or access denied");

            await ApplyGymTrendsUpsert(gym, update.Trends);

            //gym.State = GymState.NotVerified;
            gym.ModifiedMoment = DateTime.UtcNow;
            gym.GymOwnerLastUpdateMoment = DateTime.UtcNow;

            await _gymRepository.ReplaceOneAsync(gym);

            return ConvertToResult(gym);
        }


        /// <summary>
        /// use for apply gym trends upsert
        /// </summary>
        /// <param name="gym"></param>
        /// <param name="updates"></param>
        /// <returns></returns>
        /// <exception cref="BadRequestException"></exception>
        private async Task ApplyGymTrendsUpsert(
         Gym gym,
         List<GymTrendInfoUpdate> updates)
        {
            if (updates == null)
            {
                gym.Trends = [];
                gym.SupportedGender = [];
                gym.SupportedTimeType = [];
                gym.GymTotalWorkingHour = InitTotalWeek();
                return;
            }

            updates = updates
                .Where(x => !string.IsNullOrWhiteSpace(x.GymTrendId))
                .DistinctBy(x => x.GymTrendId)
                .ToList();

            var trendsData = await GetGymTrendsAsync(
                updates.Select(x => x.GymTrendId).ToList());

            var finalTrends = new List<GymTrendInfo>();

            foreach (var update in updates)
            {

                var td = trendsData
                    .FirstOrDefault(t => t.GymTrendId == update.GymTrendId) ?? throw new BadRequestException($"رشته ورزشی {update.GymTrendId} نامعتبر است");


                var existing = gym.Trends?
                    .FirstOrDefault(x => x.GymTrendId == update.GymTrendId);

                var trend = BuildOrUpdateTrend(
                    existing,
                    update,
                    td.Title,
                    td.IconUrl,
                    gym.Level);

                finalTrends.Add(trend);
            }

            gym.Trends = finalTrends;
            gym.SupportedGender = CalculateSupportedGenderForGym(gym);
            gym.SupportedTimeType = CalculateSupportedGymTimeTypeForGym(gym);
            gym.GymTotalWorkingHour = CalculateGymTotalTimeForGym(gym);
        }


        /// <summary>
        /// use for build or update trend
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        /// <param name="title"></param>
        /// <param name="gymLevel"></param>
        /// <returns></returns>
        private GymTrendInfo BuildOrUpdateTrend(
        GymTrendInfo existing,
        GymTrendInfoUpdate update,
        string title,
        string iconUrl,
        GymLevel gymLevel)
        {
            var trend = existing ?? new GymTrendInfo
            {
                GymTrendId = update.GymTrendId,

            };
            trend.TrendIconUrl = iconUrl;
            trend.Title = title;
            trend.TrendIconUrl = existing?.TrendIconUrl;
            trend.Men = BuildGenderWorkingHours(update.Men, gymLevel);
            trend.Women = BuildGenderWorkingHours(update.Women, gymLevel);

            return trend;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="whois"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<GymListResult> GetAllGymsAsync(GymSimpleFilter update, string whois)
        {
            var result = new GymListResult();

            var builder = Builders<Gym>.Filter;
            var filters = new List<FilterDefinition<Gym>>();

            ManageGymListSimpleFilters(update, builder, filters);

            filters.Add(builder.Eq(g => g.GymOwnerPublicKey, whois));

            var finalFilter = builder.And(filters);

            result.TotalCount = (int)await _gymRepository.CountAsync(finalFilter);

            if (result.TotalCount == 0)
                return result;

            var page = update.Pagination.Page <= 0 ? 1 : update.Pagination.Page;
            var size = update.Pagination.Size;
            var skip = (page - 1) * size;

            result.PageCount = (int)Math.Ceiling(
                result.TotalCount / (double)size
            );

            var gyms = await _gymRepository
                .Find(finalFilter)
                .Skip(skip)
                .Limit(size)
                .ToListAsync();

            result.Data = gyms.Select(g => new GymResult
            {
                GymId = g.GymId,
                Title = g.Title,
                Description = g.Description,
                Level = g.Level,
                SupportedGender = g.SupportedGender,
                Slug = g.Slug,
                WeekPrices = g.WeekPrices,
                Address = g.Address,
                GymTotalWorkingHour = g.GymTotalWorkingHour,

                Contact = g.Contact,
                Images = g.Images,
                Trends = g.Trends,
                Facilities = g.Facilities,

                Rate = g.Rate,

                CreatedMoment = g.CreatedMoment,
                ModifiedMoment = g.ModifiedMoment,
                State = g.State,
            }).ToList();

            return result;
        }

        #endregion





        #region Client

        /// <summary>
        /// use for getting gyms with filter
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        public async Task<GymListLightResult> GetGymsWithFilterAsync(GymFilter update)
        {
            var result = new GymListLightResult();

            var builder = Builders<Gym>.Filter;
            var filters = new List<FilterDefinition<Gym>>();

            ManageGymListFilters(update, builder, filters);


            var finalFilter = builder.And(filters);

            result.TotalCount = (int)await _gymRepository.CountAsync(finalFilter);

            if (result.TotalCount == 0)
                return result;

            var page = update.Pagination.Page <= 0 ? 1 : update.Pagination.Page;
            var size = update.Pagination.Size;
            var skip = (page - 1) * size;

            result.PageCount = (int)Math.Ceiling(
                result.TotalCount / (double)size
            );

            var gyms = await _gymRepository
                .Find(finalFilter)
                .Skip(skip)
                .Limit(size)
                .ToListAsync();

            result.Data = gyms.Select(g => new GymLightResult
            {
                GymId = g.GymId,
                Title = g.Title,
                Description = g.Description,
                Level = g.Level,
                SupportedGender = g.SupportedGender,

                Address = g.Address,
                GymTotalWorkingHour = g.GymTotalWorkingHour,

                Contact = g.Contact,
                Images = g.Images,
                Facilities = g.Facilities,
                WeekPrices = g.WeekPrices,
                Slug = g.Slug,  
                Rate = g.Rate,

                CreatedMoment = g.CreatedMoment,
                ModifiedMoment = g.ModifiedMoment,
                State = g.State,
            }).ToList();

            return result;
        }


        /// <summary>
        /// this method use for get gym
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        public async Task<GymResult> GetOneGymAsync(GymIdUpdate update)
        {
            var gym = await GetOneGymForInternalUsageAsync(update.GymId);
            return ConvertToResult(gym);
        }


        /// <summary>
        /// use for get gym data with closures by slug
        /// </summary>
        /// <param name="slug"></param>
        /// <returns></returns>
        /// <exception cref="NotFoundException"></exception>
        public async Task<GymFullResult> GetGymDataBySlugAsync(string slug)
        {
            var gym = await _gymRepository.AsQueryable()
               .Where(g => g.Slug == slug && !g.IsDeleted)
               .FirstOrDefaultAsync();

            if (gym == null)
                throw new NotFoundException("باشگاه مورد نظر یافت نشد");

            var fullResult = new GymFullResult
            {
                GymId = gym.GymId,
                Title = gym.Title,
                Description = gym.Description,
                Level = gym.Level,
                Address = gym.Address,
                Contact = gym.Contact,
                Images = gym.Images,
                State = gym.State,
                Rate = gym.Rate,
                SupportedGender = gym.SupportedGender,
                Trends = gym.Trends,
                Facilities = gym.Facilities,
                GymTotalWorkingHour = gym.GymTotalWorkingHour,
                CreatedMoment = gym.CreatedMoment,
                ModifiedMoment = gym.ModifiedMoment,
                WeekPrices = gym.WeekPrices,
                Slug = gym.Slug,

            };
            var today = DateTime.UtcNow.Date;
            var next7Days = today.AddDays(7);

            //var closureFilter = Builders<GymClosure>.Filter.And(
            //    Builders<GymClosure>.Filter.Eq(c => c.GymId, gym.GymId),
            //    Builders<GymClosure>.Filter.Gte(c => c.ClosureDate, today),
            //    Builders<GymClosure>.Filter.Lte(c => c.ClosureDate, next7Days)
            //);

            var iranTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
            var iranToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, iranTimeZone).Date;
            var fromUtc = TimeZoneInfo.ConvertTimeToUtc(iranToday, iranTimeZone);
            var toUtc = TimeZoneInfo.ConvertTimeToUtc(
                iranToday.AddDays(2).AddDays(1).AddTicks(-1),
                iranTimeZone);

            var closureFilter =
                Builders<GymClosure>.Filter.Eq(c => c.GymId, gym.GymId) &
                Builders<GymClosure>.Filter.Gte(x => x.ClosureDate, fromUtc) &
                Builders<GymClosure>.Filter.Lte(x => x.ClosureDate, toUtc);

            var closures = await _gymClosureRepository.Find(closureFilter).Limit(100).ToListAsync();

            SetUpcomingClosures(fullResult, closures);

            return fullResult;

        }


        /// <summary>
        /// this method use for add closures to result
        /// </summary>
        /// <param name="result"></param>
        /// <param name="closures"></param>
        private void SetUpcomingClosures(GymFullResult result, IEnumerable<GymClosure> closures)
        {
            result.UpcomingClosures = closures
                .Select(c => new GymClosureResult
                {
                    GymClosureId = c.GymClosureId,
                    GymId = c.GymId,
                    ClosureDate = c.ClosureDate,
                    DayOfWeek = c.DayOfWeek,
                    IsAllDay = c.IsAllDay,
                    From = c.From,
                    To = c.To,
                    Reason = c.Reason,
                    CreatedMoment = c.CreatedMoment,
                    ModifiedMoment = c.ModifiedMoment
                })
                .OrderBy(c => c.ClosureDate)
                .ToList();
        }


        /// <summary>
        ///  this method use to manage filters for gym list
        /// </summary>
        /// <param name="update"></param>
        /// <param name="builder"></param>
        /// <param name="filters"></param>
        private static void ManageGymListFilters(GymFilter update, FilterDefinitionBuilder<Gym> builder, List<FilterDefinition<Gym>> filters)
        {

            if (update.Nearest != null)
            {
                //var point = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                //    new GeoJson2DGeographicCoordinates(
                //        update.Nearest.Longitude,
                //        update.Nearest.Latitude
                //    )
                //);
                //filters.Add(builder.Exists("Address.Location"));
                //filters.Add(builder.Near(
                //    g => g.Address.Location,
                //    point,
                //    update.Nearest.MaxDistanceMeters
                //));

                filters.Add(builder.Exists(g => g.Address.Location));

                filters.Add(
                    builder.GeoWithinCenterSphere(
                        g => g.Address.Location,
                        update.Nearest.Longitude,
                        update.Nearest.Latitude,
                        update.Nearest.MaxDistanceMeters / 6378137.0
                    )
                );
            }

            ManageGymListSimpleFilters(update, builder, filters);

        }


        /// <summary>
        /// use for get gym list with filter
        /// </summary>
        /// <param name="update"></param>
        /// <param name="builder"></param>
        /// <param name="filters"></param>
        private static void ManageGymListSimpleFilters(GymSimpleFilter update, FilterDefinitionBuilder<Gym> builder, List<FilterDefinition<Gym>> filters)
        {


            if (update.Genders != null && update.Genders.Count != 0)
            {
                filters.Add(builder.AnyIn(g => g.SupportedGender, update.Genders));
            }


            if (update.GymLevels != null && update.GymLevels.Count != 0)
            {
                filters.Add(builder.In(g => g.Level, update.GymLevels));
            }


            if (update.GymTrendIds != null && update.GymTrendIds.Count != 0)
            {
                filters.Add(builder.ElemMatch(
                    g => g.Trends,
                    t => update.GymTrendIds.Contains(t.GymTrendId)
                ));
            }


            if (update.FacilityIds != null && update.FacilityIds.Count != 0)
            {
                filters.Add(builder.ElemMatch(
                    g => g.Facilities,
                    f => update.FacilityIds.Contains(f.FacilityId)
                ));
            }


            if (!string.IsNullOrWhiteSpace(update.Search))
            {
                var regex = new BsonRegularExpression(update.Search, "i");

                filters.Add(builder.Or(
                    builder.Regex(g => g.Title, regex),
                    builder.Regex(g => g.Description, regex)
                ));
            }

        }

        #endregion




        #region Admin
        /// <summary>
        /// this method use for create gym by admin
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        public async Task<GymAdminResult> AddGymByAdminAsync(AddGymByAdminUpdate update)
        {
            var gymResult = await AddGymAsync(new AddGymUpdate
            {
                Title = update.Title,
                Description = update.Description,
                Level = update.Level,
                Address = update.Address,
                Contact = update.Contact,
                Images = update.Images,
                Trends = update.Trends,
                FacilityIds = update.FacilityIds
            }, update.GymOwnerPublicKey);

            return ConvertToAdminResult(gymResult, update.GymOwnerPublicKey);
        }


        /// <summary>
        /// this method use for edit gym by admin
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        public async Task<GymAdminResult> EditGymByAdminAsync(EditGymByAdminUpdate update)
        {
            var gymResult = await EditGymAsync(new EditGymUpdate
            {
                GymId = update.GymId,
                Title = update.Title,
                Description = update.Description,
                //Level = update.Level,
                Address = update.Address,
                Contact = update.Contact,
                Images = update.Images,
                //Trends = update.Trends,
                FacilityIds = update.FacilityIds
            }, update.GymOwnerPublicKey);

            return ConvertToAdminResult(gymResult, update.GymOwnerPublicKey);

        }


        /// <summary>
        /// this method use for get all data for admin
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <exception cref="BaseException"></exception>
        public async Task<MonjoFilteredResult<GymAdminResult>> GetAllGymsForAdminAsync(MonjoQuery query)
        {
            try
            {
                query.WithBase<GymAdminResult>();

                var data = await _gymRepository.AsQueryable()
                   .Apply(query.Where)
                   .Apply(query.Order)
                   .Select(gymResult => new GymAdminResult
                   {
                       GymId = gymResult.GymId,
                       Title = gymResult.Title,
                       Description = gymResult.Description,
                       Level = gymResult.Level,
                       Address = gymResult.Address,
                       Contact = gymResult.Contact,
                       Images = gymResult.Images,
                       Trends = gymResult.Trends,
                       Facilities = gymResult.Facilities,
                       SupportedGender = gymResult.SupportedGender,
                       State = gymResult.State,
                       Rate = gymResult.Rate,
                       GymTotalWorkingHour = gymResult.GymTotalWorkingHour,
                       CreatedMoment = gymResult.CreatedMoment,
                       ModifiedMoment = gymResult.ModifiedMoment,
                       GymOwnerPublicKey = gymResult.GymOwnerPublicKey,
                       Slug = gymResult.Slug,
                       WeekPrices = gymResult.WeekPrices
                   })
                   .ExecuteAsync(query);

                return data;
            }
            catch (Exception)
            {
                throw new BaseException();
            }
        }

        /// <summary>
        /// this method use for remove gym by admin
        /// </summary>
        /// <param name="gymIdUpdate"></param>
        /// <returns></returns>
        public async Task<GymAdminResult> RemoveGymByAdminAsync(GymIdUpdate gymIdUpdate)
        {
            var gym = await GetOneGymForInternalUsageAsync(gymIdUpdate.GymId);
            await _gymRepository.DeleteOneAsync(q => q.GymId == gym.Id);
            return ConvertToAdminResult(gym);
        }


        /// <summary>
        /// use for upsert gym trend data by admin
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        /// <exception cref="BadRequestException"></exception>
        public async Task<GymAdminResult> UpsertGymTrendsByAdminAsync(
         UpsertGymTrendsUpdateByAdmin update)
        {
            var gym = await _gymRepository.AsQueryable()
                .Where(x => x.GymId == update.GymId &&
                            x.GymOwnerPublicKey == update.GymOwnerPublicKey)
                .FirstOrDefaultAsync();


            if (gym == null)
                throw new BadRequestException("Gym not found or access denied");

            await ApplyGymTrendsUpsert(gym, update.Trends);

            gym.ModifiedMoment = DateTime.UtcNow;
            gym.GymOwnerLastUpdateMoment = DateTime.UtcNow;

            await _gymRepository.ReplaceOneAsync(gym);

            return ConvertToAdminResult(gym);
        }


        /// <summary>
        /// use for convert gym to admin result
        /// </summary>
        /// <param name="gymResult"></param>
        /// <param name="gymOwnerPublicKey"></param>
        /// <returns></returns>
        private GymAdminResult ConvertToAdminResult(GymResult gymResult, string gymOwnerPublicKey)
        {
            return new GymAdminResult
            {
                GymId = gymResult.GymId,
                Title = gymResult.Title,
                Description = gymResult.Description,
                Level = gymResult.Level,
                Address = gymResult.Address,
                Contact = gymResult.Contact,
                Images = gymResult.Images,
                Trends = gymResult.Trends,
                Facilities = gymResult.Facilities,
                SupportedGender = gymResult.SupportedGender,
                State = gymResult.State,
                Rate = gymResult.Rate,
                GymTotalWorkingHour = gymResult.GymTotalWorkingHour,
                CreatedMoment = gymResult.CreatedMoment,
                ModifiedMoment = gymResult.ModifiedMoment,
                GymOwnerPublicKey = gymOwnerPublicKey,
                Slug = gymResult.Slug,
                WeekPrices = gymResult.WeekPrices

            };
        }


        /// <summary>
        ///  use for convert gym to admin result
        /// </summary>
        /// <param name="gymResult"></param>
        /// <returns></returns>
        private GymAdminResult ConvertToAdminResult(Gym gymResult)
        {
            return new GymAdminResult
            {
                GymId = gymResult.GymId,
                Title = gymResult.Title,
                Description = gymResult.Description,
                Level = gymResult.Level,
                Address = gymResult.Address,
                Contact = gymResult.Contact,
                Images = gymResult.Images,
                Trends = gymResult.Trends,
                Facilities = gymResult.Facilities,
                SupportedGender = gymResult.SupportedGender,
                State = gymResult.State,
                Rate = gymResult.Rate,
                GymTotalWorkingHour = gymResult.GymTotalWorkingHour,
                CreatedMoment = gymResult.CreatedMoment,
                ModifiedMoment = gymResult.ModifiedMoment,
                GymOwnerPublicKey = gymResult.GymOwnerPublicKey,
                Slug = gymResult.Slug,
                WeekPrices = gymResult.WeekPrices

            };
        }




        #endregion


        #region Internal

        /// <summary>
        /// get one gym for internal usage
        /// </summary>
        /// <param name="gymId"></param>
        /// <returns></returns>
        /// <exception cref="NotFoundException"></exception>
        public async Task<Gym> GetOneGymForInternalUsageAsync(string gymId)
        {
            var gym = await _gymRepository.AsQueryable().Where(q => q.GymId == gymId).FirstOrDefaultAsync() ??
                throw new NotFoundException("باشگاه یافت نشد");

            return gym;
        }


        /// <summary>
        /// use for check facility id in gyms 
        /// </summary>
        /// <param name="facilityId"></param>
        /// <returns></returns>
        /// <exception cref="BadRequestException"></exception>
        public async Task<bool> IsFacilityUsedAsync(string facilityId)
        {
            if (string.IsNullOrWhiteSpace(facilityId))
                throw new BadRequestException("FacilityId is required", nameof(facilityId));

            return await _gymRepository.AsQueryable().AnyAsync(g => g.Facilities.Any(f => f.FacilityId == facilityId));
        }

        /// <summary>
        /// use for update facility in all gyms
        /// </summary>
        /// <param name="facilityId"></param>
        /// <param name="newTitle"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task UpdateFacilityTitleAsync(string facilityId, string newTitle)
        {
            try
            {


                if (string.IsNullOrWhiteSpace(facilityId))
                    throw new ArgumentException("FacilityId is required", nameof(facilityId));

                if (string.IsNullOrWhiteSpace(newTitle))
                    throw new ArgumentException("New title is required", nameof(newTitle));

                var filter = Builders<Gym>.Filter.ElemMatch(g => g.Facilities, f => f.FacilityId == facilityId);

                var update = Builders<Gym>.Update.Set("Facilities.$.Title", newTitle);

                await _gymRepository.UpdateManyAsync(filter, update);
            }
            catch (Exception ex)
            {
                throw new BaseException(ex.Message);
            }
        }


        /// <summary>
        /// this method check terndId existing in gyms
        /// </summary>
        /// <param name="trendId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<bool> IsTrendUsedAsync(string trendId)
        {
            if (string.IsNullOrWhiteSpace(trendId))
                throw new ArgumentException("TrendId is required", nameof(trendId));

            return await _gymRepository.AsQueryable()
                .AnyAsync(g => g.Trends.Any(t => t.GymTrendId == trendId));
        }


        /// <summary>
        /// use for update gym trend title
        /// </summary>
        /// <param name="trendId"></param>
        /// <param name="newTitle"></param>
        /// <returns></returns>
        /// <exception cref="BadRequestException"></exception>
        /// <exception cref="BaseException"></exception>
        public async Task UpdateGymTrendTitleAsync(string trendId, string newTitle)
        {
            if (string.IsNullOrWhiteSpace(trendId))
                throw new BadRequestException("TrendId is required", nameof(trendId));

            if (string.IsNullOrWhiteSpace(newTitle))
                throw new BadRequestException("New title is required", nameof(newTitle));

            var filter = Builders<Gym>.Filter.ElemMatch(g => g.Trends, t => t.GymTrendId == trendId);

            var update = Builders<Gym>.Update.Set("Trends.$[t].Title", newTitle);

            var arrayFilters = new List<ArrayFilterDefinition>
            {
                new JsonArrayFilterDefinition<Gym>($"{{ 't.GymTrendId': '{trendId}' }}")
            };

            var options = new UpdateOptions { ArrayFilters = arrayFilters };

            try
            {
                await _gymRepository.UpdateManyAsync(filter, update, options);
            }
            catch (Exception ex)
            {
                throw new BaseException("Error while updating gym trend title", ex);
            }
        }


        /// <summary>
        /// use for sync rate with used attendance
        /// </summary>
        /// <param name="gymId"></param>
        /// <returns></returns>
        /// <exception cref="BadRequestException"></exception>
        /// <exception cref="BaseException"></exception>
        public async Task SyncRateOfGymAsync(string gymId)
        {

            try
            {
                if (string.IsNullOrWhiteSpace(gymId))
                    throw new BadRequestException("شناسه باشگاه معتبر نیست");

                var query = _gymAttendanceRepository.AsQueryable()
                    .Where(x =>
                        x.GymId == gymId &&
                        x.GymAttendanceState == GymAttendanceState.Used &&
                        x.GivenRate.HasValue
                    );

                var count = await query.CountAsync();

                decimal averageRate = 0;

                if (count > 0)
                {
                    averageRate = await query.AverageAsync(x => x.GivenRate!.Value);
                }

                var update = Builders<Gym>.Update
                    .Set(x => x.Rate, Math.Round(averageRate, 2))
                    .Set(x => x.RateCount, count);

                var result = await _gymRepository.FindOneAndUpdateAsync(
                    x => x.GymId == gymId,
                    update
                );
            }
            catch (Exception)
            {
                throw new BaseException("Error while syncing gym rate");
            }


        }


        public async Task IncreaseSessionAvailableCapacityAsync(
            string gymId,
            string gymSessionId,
            bool isMenSession)
        {
            var sessionPath = isMenSession
                ? "Trends.$[].Men.$[].Sessions.$[s]"
                : "Trends.$[].Women.$[].Sessions.$[s]";

            var filter = Builders<Gym>.Filter.Eq(g => g.GymId, gymId);

            var update = Builders<Gym>.Update.Inc($"{sessionPath}.UsedCapacity", 1); 

            var arrayFilters = new List<ArrayFilterDefinition>
            {
                new BsonDocumentArrayFilterDefinition<BsonDocument>(
                    new BsonDocument
                    {
                        { "s.GymSessionId", gymSessionId },
                        {
                            "$expr",
                            new BsonDocument("$lt", new BsonArray
                            {
                                "$s.UsedCapacity",
                                "$s.Capacity"
                            })
                        }
                    })
            };

            var result = await _gymRepository.UpdateManyAsync(
                filter,
                update,
                new UpdateOptions { ArrayFilters = arrayFilters }
            );

            if (result.ModifiedCount == 0)
                throw new BadRequestException("ظرفیت این جلسه تکمیل شده است");
        }


        /// <summary>
        /// Update all gyms that use this facility with new Title
        /// </summary>
        public async Task UpdateGymsWithFacilityAsync(GymFacility updatedFacility)
        {
            if (updatedFacility == null)
                throw new ArgumentNullException(nameof(updatedFacility));

            var filter = Builders<Gym>.Filter.ElemMatch(
                g => g.Facilities,
                f => f.FacilityId == updatedFacility.FacilityId
            );

            var update = Builders<Gym>.Update
                .Set("Facilities.$[f].Title", updatedFacility.Title);

            var arrayFilters = new List<ArrayFilterDefinition>
            {
                new BsonDocumentArrayFilterDefinition<BsonDocument>(
                    new BsonDocument("f.FacilityId", updatedFacility.FacilityId)
                )
            };

            var options = new UpdateOptions { ArrayFilters = arrayFilters };

            await _gymRepository.UpdateManyAsync(filter, update, options);
        }


        public async Task UpdateGymsWithTrendAsync(GymTrend updatedTrend)
        {
            if (updatedTrend == null)
                throw new ArgumentNullException(nameof(updatedTrend));

            var filter = Builders<Gym>.Filter.ElemMatch(
                g => g.Trends,
                t => t.GymTrendId == updatedTrend.GymTrendId
            );

            var update = Builders<Gym>.Update
                .Set("Trends.$[t].Title", updatedTrend.Title)
                .Set("Trends.$[t].TrendIconUrl", updatedTrend.IconUrl);

            var arrayFilters = new List<ArrayFilterDefinition>
            {
                new BsonDocumentArrayFilterDefinition<BsonDocument>(
                    new BsonDocument("t.GymTrendId", updatedTrend.GymTrendId)
                )
            };

            var options = new UpdateOptions { ArrayFilters = arrayFilters };

            await _gymRepository.UpdateManyAsync(filter, update, options);
        }

        #endregion


        /// <summary>
        /// this method use to convert gym to gym result
        /// </summary>
        /// <param name="gym"></param>
        /// <returns></returns>
        private GymResult ConvertToResult(Gym gym)
        {
            if (gym == null)
                return null;

            return new GymResult
            {
                GymId = gym.GymId,

                Title = gym.Title,
                Description = gym.Description,
                Level = gym.Level,

                SupportedGender = gym.SupportedGender,

                Address = gym.Address,
                GymTotalWorkingHour = gym.GymTotalWorkingHour,

                Contact = gym.Contact,
                Images = gym.Images,
                Trends = gym.Trends,
                Facilities = gym.Facilities,

                State = gym.State,
                Rate = gym.Rate,
                Slug = gym.Slug,
                CreatedMoment = gym.CreatedMoment,
                ModifiedMoment = gym.ModifiedMoment,
                WeekPrices = gym.WeekPrices
            };
        }


        /// <summary>
        /// this method use for create slug by random seri and title of gym
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private string CreateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be null or empty.", nameof(title));

            var randomStr = randomService.GetSecureAlphaNumericString(10);

            var slugTitle = title.Trim().ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("/", "-")
                .Replace("\\", "-");

            var slug = $"{randomStr}-{slugTitle}";

            return slug;
        }

        public async Task UndoGymCapacityByAttendanceAsync(string depositReference)
        {
            var attendance = await _gymAttendanceRepository.AsQueryable()
                .FirstOrDefaultAsync(q =>
                    q.DepositReference.ToLower() == depositReference.ToLower());

            if (attendance == null)
                return;

            if (attendance.GymTimeType == GymTimeType.FreeTime)
                return;

            var gym = await _gymRepository.AsQueryable()
                .FirstOrDefaultAsync(q => q.GymId == attendance.GymId);

            if (gym == null || gym.Trends == null)
                return;

            bool? isMenSession = null;

            foreach (var trend in gym.Trends)
            {
                if (trend.Men != null)
                {
                    var foundInMen = trend.Men
                        .SelectMany(d => d.Sessions ?? Enumerable.Empty<GymSession>())
                        .Any(s => s.GymSessionId == attendance.GymSessionId);

                    if (foundInMen)
                    {
                        isMenSession = true;
                        break;
                    }
                }

                if (trend.Women != null)
                {
                    var foundInWomen = trend.Women
                        .SelectMany(d => d.Sessions ?? Enumerable.Empty<GymSession>())
                        .Any(s => s.GymSessionId == attendance.GymSessionId);

                    if (foundInWomen)
                    {
                        isMenSession = false;
                        break;
                    }
                }
            }

            if (isMenSession == null)
                return; 

            await DecreaseSessionUsedCapacityAsync(
                attendance.GymId,
                attendance.GymSessionId,
                isMenSession.Value
            );
        }

        public async Task DecreaseSessionUsedCapacityAsync(
        string gymId,
        string gymSessionId,
        bool isMenSession)
            {
                var sessionPath = isMenSession
                    ? "Trends.$[].Men.$[].Sessions.$[s]"
                    : "Trends.$[].Women.$[].Sessions.$[s]";

                var filter = Builders<Gym>.Filter.Eq(g => g.GymId, gymId);

                var update = Builders<Gym>.Update.Inc($"{sessionPath}.UsedCapacity", -1);

                var arrayFilters = new List<ArrayFilterDefinition>
                {
                    new BsonDocumentArrayFilterDefinition<BsonDocument>(
                        new BsonDocument
                        {
                            { "s.GymSessionId", gymSessionId },
                            {
                                "$expr",
                                new BsonDocument("$gt", new BsonArray
                                {
                                    "$s.UsedCapacity",
                                    0
                                })
                            }
                        })
                };

                var result = await _gymRepository.UpdateManyAsync(
                    filter,
                    update,
                    new UpdateOptions { ArrayFilters = arrayFilters }
                );

                if (result.ModifiedCount == 0)
                    _logger.LogInformation("ظرفیت استفاده‌شده‌ای برای کاهش وجود ندارد");
            }


    }
}



///// <summary>
///// this method use for handle gym trends
///// </summary>
///// <param name="gym"></param>
///// <param name="trends"></param>
///// <returns></returns>
///// 
//private async Task HandleGymTrends(Gym gym, List<GymTrendInfoUpdate> trends)
//{
//    try
//    {
//        if (trends == null || trends.Count == 0)
//        {
//            gym.Trends = new List<GymTrendInfo>();
//            gym.SupportedGender = new List<Gender>();
//            gym.GymTotalWorkingHour = InitWeek();
//            return;
//        }

//        trends = trends
//            .Where(t => !string.IsNullOrWhiteSpace(t.GymTrendId))
//            .DistinctBy(t => t.GymTrendId)
//            .ToList();


//        var supportedGender = new HashSet<Gender>();
//        var totalWeek = InitWeek();

//        var trendTitles = await GetGymTrendsAsync(trends.Select(t => t.GymTrendId).ToList());
//        var titleMap = trendTitles.ToDictionary(t => t.GymTrendId, t => t.Title);

//        var finalTrends = new List<GymTrendInfo>();

//        foreach (var input in trends)
//        {
//            var trend = new GymTrendInfo
//            {
//                GymTrendId = input.GymTrendId,
//                Title = titleMap.GetValueOrDefault(input.GymTrendId),

//                Men = BuildGenderWorkingHours(input.Men, Gender.Male, supportedGender, totalWeek),
//                Women = BuildGenderWorkingHours(input.Women, Gender.Female, supportedGender, totalWeek)
//            };

//            finalTrends.Add(trend);
//        }

//        gym.Trends = finalTrends;
//        gym.SupportedGender = supportedGender.ToList();
//        gym.GymTotalWorkingHour = totalWeek;
//    }
//    catch (Exception e)
//    {
//        throw new Exception("Error while handling gym trends", e);
//    }
//}




///// <summary>
///// this method use for build gender working hours
///// </summary>
///// <param name="input"></param>
///// <param name="gender"></param>
///// <param name="supportedGender"></param>
///// <param name="totalWeek"></param>
///// <returns></returns>
//private GenderWorkingHours BuildGenderWorkingHours(
//    GenderWorkingHours input,
//    Gender gender,
//    HashSet<Gender> supportedGender,
//    List<GymTrendWorkingHour> totalWeek)
//{
//    var result = new GenderWorkingHours
//    {
//        IsActive = input != null,
//        WorkingHours = NormalizeWeek(input?.WorkingHours)
//    };

//    if (result.IsActive && result.WorkingHours.Any(d => !d.IsClosed))
//        supportedGender.Add(gender);

//    foreach (var day in result.WorkingHours.Where(d => !d.IsClosed))
//    {
//        var totalDay = totalWeek.First(x => x.DayOfWeek == day.DayOfWeek);

//        totalDay.IsClosed = false;

//        totalDay.From = totalDay.From.HasValue
//            ? Math.Min(totalDay.From.Value, day.From.Value)
//            : day.From;

//        totalDay.To = totalDay.To.HasValue
//            ? Math.Max(totalDay.To.Value, day.To.Value)
//            : day.To;
//    }

//    return result;
//}




//private List<GymTrendWorkingHour> InitWeek()
//{
//    var result = new List<GymTrendWorkingHour>();

//    foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
//    {
//        result.Add(new GymTrendWorkingHour
//        {
//            DayOfWeek = day,
//            IsClosed = true,
//            From = null,
//            To = null
//        });
//    }

//    return result;
//}


//private List<GymTrendWorkingHour> NormalizeWeek(List<GymTrendWorkingHour> input)
//{
//    var result = InitWeek();

//    if (input == null)
//        return result;

//    foreach (var day in result)
//    {
//        var source = input.FirstOrDefault(x => x.DayOfWeek == day.DayOfWeek);
//        if (source == null) continue;

//        day.IsClosed = source.IsClosed;
//        day.From = source.From;
//        day.To = source.To;
//    }

//    return result;
//}
//public async Task<GymResult> EditGymAsync(EditGymUpdate update, string whois)
//{
//    var gym = await _gymRepository.AsQueryable()
//        .Where(x => x.GymId == update.GymId && x.GymOwnerPublicKey == whois)
//        .FirstOrDefaultAsync();

//    if (gym == null)
//        throw new Exception("Gym not found");

//    if (gym.GymOwnerPublicKey != whois)
//        throw new Exception("Access denied");


//    if (gym.Title != update.Title.Trim())
//    {
//        gym.Title = update.Title.Trim();
//        gym.Slug = CreateSlug(gym.Slug);
//    }

//    gym.Description = update.Description.Trim();
//    //gym.Level = update.Level;
//    gym.Address = CreateAddressInfo(update.Address);
//    gym.Contact = update.Contact;
//    gym.Images = update.Images;



//    if (update.FacilityIds != null && update.FacilityIds.Any())
//    {
//        await GetFacilitiesAsync(gym, update.FacilityIds);
//    }
//    else
//    {
//        gym.Facilities = [];
//    }

//    if (gym.Price != update.Price)
//    {
//        if (gym.PriceTrackerDatetime > DateTime.UtcNow.AddDays(-7))
//            throw new BadRequestException("ویرایش قیمت فقط یکبار در هفته مجاز می باشد");
//        ValidatePriceWithLevel(update.Price, gym.Level);
//        gym.Price = update.Price;
//        gym.PriceTrackerDatetime = DateTime.UtcNow;
//    }


//    if (update.Trends != null && update.Trends.Any())
//    {
//        await HandleGymTrends(gym, update.Trends);
//    }
//    else
//    {
//        gym.Trends = [];
//        gym.SupportedGender = [];
//        gym.GymTotalWorkingHour = InitTotalWeek();
//    }

//    gym.State = GymState.NotVerified;
//    gym.ModifiedMoment = DateTime.UtcNow;

//    await _gymRepository.ReplaceOneAsync(gym);

//    return ConvertToResult(gym);
//}