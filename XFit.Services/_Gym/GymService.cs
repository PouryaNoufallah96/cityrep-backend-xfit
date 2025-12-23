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
                Address = update.Address,
                Contact = update.Contact,
                Images = update.Images,
                State = GymState.NotVerified,
                Slug = CreateSlug(update.Title),
                GymOwnerPublicKey = whois
            };

            await GetFacilitiesAsync(newGym, update.FacilityIds);

            await HandleGymTrends(newGym, update.Trends);

            await _gymRepository.InsertOneAsync(newGym);
            return ConvertToResult(newGym);
        }


        /// <summary>
        /// this method use for handle gym trends
        /// </summary>
        /// <param name="gym"></param>
        /// <param name="trends"></param>
        /// <returns></returns>
        /// 
        private async Task HandleGymTrends(Gym gym, List<GymTrendInfoUpdate> trends)
        {
            try
            {
                if (trends == null || trends.Count == 0)
                {
                    gym.Trends = new List<GymTrendInfo>();
                    gym.SupportedGender = new List<Gender>();
                    gym.GymTotalWorkingHour = InitWeek();
                    return;
                }

                trends = trends
                    .Where(t => !string.IsNullOrWhiteSpace(t.GymTrendId))
                    .DistinctBy(t => t.GymTrendId)
                    .ToList();


                var supportedGender = new HashSet<Gender>();
                var totalWeek = InitWeek();

                var trendTitles = await GetGymTrendsAsync(trends.Select(t => t.GymTrendId).ToList());
                var titleMap = trendTitles.ToDictionary(t => t.GymTrendId, t => t.Title);

                var finalTrends = new List<GymTrendInfo>();

                foreach (var input in trends)
                {
                    var trend = new GymTrendInfo
                    {
                        GymTrendId = input.GymTrendId,
                        Title = titleMap.GetValueOrDefault(input.GymTrendId),

                        Men = BuildGenderWorkingHours(input.Men, Gender.Male, supportedGender, totalWeek),
                        Women = BuildGenderWorkingHours(input.Women, Gender.Female, supportedGender, totalWeek)
                    };

                    finalTrends.Add(trend);
                }

                gym.Trends = finalTrends;
                gym.SupportedGender = supportedGender.ToList();
                gym.GymTotalWorkingHour = totalWeek;
            }
            catch (Exception e)
            {
                throw new Exception("Error while handling gym trends", e);
            }
        }


       

        /// <summary>
        /// this method use for build gender working hours
        /// </summary>
        /// <param name="input"></param>
        /// <param name="gender"></param>
        /// <param name="supportedGender"></param>
        /// <param name="totalWeek"></param>
        /// <returns></returns>
        private GenderWorkingHours BuildGenderWorkingHours(
            GenderWorkingHours input,
            Gender gender,
            HashSet<Gender> supportedGender,
            List<GymTrendWorkingHour> totalWeek)
        {
            var result = new GenderWorkingHours
            {
                IsActive = input != null,
                WorkingHours = NormalizeWeek(input?.WorkingHours)
            };

            if (result.IsActive && result.WorkingHours.Any(d => !d.IsClosed))
                supportedGender.Add(gender);

            foreach (var day in result.WorkingHours.Where(d => !d.IsClosed))
            {
                var totalDay = totalWeek.First(x => x.DayOfWeek == day.DayOfWeek);

                totalDay.IsClosed = false;

                totalDay.From = totalDay.From.HasValue
                    ? Math.Min(totalDay.From.Value, day.From.Value)
                    : day.From;

                totalDay.To = totalDay.To.HasValue
                    ? Math.Max(totalDay.To.Value, day.To.Value)
                    : day.To;
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
                    IsClosed = true,
                    From = null,
                    To = null
                });
            }

            return result;
        }


        /// <summary>
        /// this method use for normalize week working hours
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private List<GymTrendWorkingHour> NormalizeWeek(List<GymTrendWorkingHour> input)
        {
            var result = InitWeek();

            if (input == null)
                return result;

            foreach (var day in result)
            {
                var source = input.FirstOrDefault(x => x.DayOfWeek == day.DayOfWeek);
                if (source == null) continue;

                day.IsClosed = source.IsClosed;
                day.From = source.From;
                day.To = source.To;
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
                throw new Exception("Gym not found");

            if (gym.GymOwnerPublicKey != whois)
                throw new Exception("Access denied");


            if (gym.Title != update.Title.Trim())
            {
                gym.Title = update.Title.Trim();
                gym.Slug = CreateSlug(gym.Slug);
            }

            gym.Description = update.Description.Trim();
            gym.Level = update.Level;
            gym.Address = update.Address;
            gym.Contact = update.Contact;
            gym.Images = update.Images;



            if (update.FacilityIds != null && update.FacilityIds.Any())
            {
                await GetFacilitiesAsync(gym, update.FacilityIds);
            }
            else
            {
                gym.Facilities = [];
            }

            if (update.Trends != null && update.Trends.Any())
            {
                await HandleGymTrends(gym, update.Trends);
            }
            else
            {
                gym.Trends = [];
                gym.SupportedGender = [];
                gym.GymTotalWorkingHour = InitWeek();
            }

            gym.State = GymState.NotVerified;
            gym.ModifiedMoment = DateTime.UtcNow;

            await _gymRepository.ReplaceOneAsync(gym);

            return ConvertToResult(gym);
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
        public async Task<GymListResult> GetGymsWithFilterAsync(GymFilter update)
        {
            var result = new GymListResult();

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

            result.Data = gyms.Select(g => new GymResult
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
                Trends = g.Trends,
                Facilities = g.Facilities,

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
                ModifiedMoment = gym.ModifiedMoment
            };
            var today = DateTime.UtcNow.Date;
            var next7Days = today.AddDays(7);

            var closureFilter = Builders<GymClosure>.Filter.And(
                Builders<GymClosure>.Filter.Eq(c => c.GymId, gym.GymId),
                Builders<GymClosure>.Filter.Gte(c => c.ClosureDate, today),
                Builders<GymClosure>.Filter.Lte(c => c.ClosureDate, next7Days)
            );

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
                var point = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                    new GeoJson2DGeographicCoordinates(
                        update.Nearest.Longitude,
                        update.Nearest.Latitude
                    )
                );
                filters.Add(builder.Exists("Address.Location"));
                filters.Add(builder.Near(
                    g => g.Address.Location,
                    point,
                    update.Nearest.MaxDistanceMeters
                ));
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
            };
        }
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



        public async Task SyncRateOfGymAsync(string gymId)
        {

            try
            {
                if (string.IsNullOrWhiteSpace(gymId))
                    throw new BadRequestException("شناسه باشگاه معتبر نیست");

                var query = _gymAttendanceRepository.AsQueryable()
                    .Where(x =>
                        x.GymId == gymId &&
                        x.PaymentState == GymAttendanceState.Paid &&
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
                ModifiedMoment = gym.ModifiedMoment
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

       
    }
}
