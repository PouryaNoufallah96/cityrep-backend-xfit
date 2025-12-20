using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using Xfit.Domain.Collections;
using Xfit.Domain.Common;
using MongoDB.Driver.Linq;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Gym.DTOs.Results;
using XFit.Services._Gym.DTOs.Updates;
using XFit.Utilities.Exceptions.Common;
using XFit.Utilities.MongoDatabase.Filter;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._Gym
{
    public class GymService(IGymRepository _gymRepository,
        IGymTrendRepository _gymTrendRepository,
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
                State = GymState.NotVerified
            };

            await GetFacilitiesAsync(newGym, update.FacilityIds);

            await HandleGymTrends(newGym, update.Trends);

            await _gymRepository.InsertOneAsync(newGym);
            return c
        }


        /// <summary>
        /// this method use for handle gym trends
        /// </summary>
        /// <param name="gym"></param>
        /// <param name="trends"></param>
        /// <returns></returns>
        private async Task HandleGymTrends(Gym gym, List<GymTrendInfoUpdate> trends)
        {

            try
            {
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

                        Men = BuildGenderWorkingHours(
                            input.Men,
                            Gender.Male,
                            supportedGender,
                            totalWeek
                        ),

                        Women = BuildGenderWorkingHours(
                            input.Women,
                            Gender.Female,
                            supportedGender,
                            totalWeek
                        )
                    };

                    finalTrends.Add(trend);
                }

                gym.Trends = finalTrends;
                gym.SupportedGender = supportedGender.ToList();
                gym.GymTotalWorkingHour = totalWeek;
            }
            catch (Exception e)
            {
                throw new Exception("Error while handling gym trends");
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
                var facilities = await _gymFacilityRepository
                .Find(Builders<GymFacility>.Filter.In(f => f.FacilityId, facilityIds))
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

                throw new BaseException("Error occurred in facilities");
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




        public async Task<GymResult> EditGymAsync(EditGymUpdate update, string whois)
        {
            var gym = await _gymRepository.AsQueryable()
                .Where(x => x.GymId == update.GymId)
                .FirstOrDefaultAsync();

            if (gym == null)
                throw new Exception("Gym not found");

            if (gym.GymOwnerPublicKey != whois)
                throw new Exception("Access denied");

            gym.Title = update.Title.Trim();
            gym.Description = update.Description.Trim();
            gym.Level = update.Level;
            gym.Address = update.Address;
            gym.Contact = update.Contact;
            gym.Images = update.Images;

            // 4️⃣ Facilities
            if (update.FacilityIds != null && update.FacilityIds.Any())
            {
               await GetFacilitiesAsync(gym,update.FacilityIds);
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

        public Task<GymListResult> GetAllGymsAsync(string whois)
        {
            throw new NotImplementedException();
        }

        #endregion





        #region Client
        public Task<GymResult> GetOneGymAsync()
        {
            throw new NotImplementedException();
        }


        public Task<GymListResult> GetAllGymsAsync()
        {
            throw new NotImplementedException();
        }




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
                GymTrendWorkingHour = g.GymTotalWorkingHour,

                Contact = g.Contact,
                Images = g.Images,
                Trends = g.Trends,
                Facilities = g.Facilities,

                Rate = g.Rate,

                CreatedMoment = g.CreatedMoment,
                ModifiedMoment = g.ModifiedMoment
            }).ToList();

            return result;
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


            if (update.Genders != null && update.Genders.Any())
            {
                filters.Add(builder.AnyIn(g => g.SupportedGender, update.Genders));
            }


            if (update.GymLevels != null && update.GymLevels.Any())
            {
                filters.Add(builder.In(g => g.Level, update.GymLevels));
            }


            if (update.GymTrendIds != null && update.GymTrendIds.Any())
            {
                filters.Add(builder.ElemMatch(
                    g => g.Trends,
                    t => update.GymTrendIds.Contains(t.GymTrendId)
                ));
            }


            if (update.FacilityIds != null && update.FacilityIds.Any())
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

        public Task<Gym> AddGymByAdminAsync(AddGymByAdminUpdate update, string whois)
        {
            throw new NotImplementedException();
        }

        public Task<Gym> EditGymByAdminAsync(EditGymByAdminUpdate update, string whois)
        {
            throw new NotImplementedException();
        }

        public Task<MonjoFilteredResult<Gym>> GetAllGymsForAdminAsync(MonjoQuery query)
        {
            throw new NotImplementedException();
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

                CreatedMoment = gym.CreatedMoment,
                ModifiedMoment = gym.ModifiedMoment
            };
        }

    }
}
