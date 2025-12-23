using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.ComponentModel.DataAnnotations;
using Xfit.Domain.Collections;
using Xfit.Domain.Repositories;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Gym;
using XFit.Services._GymClosure.DTOs;
using XFit.Utilities.Exceptions.Common;
using XFit.Utilities.MongoDatabase.Extensions;
using XFit.Utilities.MongoDatabase.Filter;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._GymClosure
{
    public class GymClosureService(IGymClosureRepository _gymClosureRepository, IGymService _gymService) : IGymClosureService, IScopedDependency
    {


        #region GymOwner Side

        public async Task<GymClosureResult> CreateAsync(GymClosureCreateUpdate update, string whois)
        {

            if (await HasTimeConflictAsync(update.GymId, update.ClosureDate.ToDateTime(TimeOnly.MinValue), update.From, update.To))
                throw new ValidationException("خطا: در این تاریخ و ساعت، تعطیلی دیگری ثبت شده است.");

            var gym = await _gymService.GetOneGymForInternalUsageAsync(update.GymId);
            if (gym.GymOwnerPublicKey != whois) throw new NotFoundException("باشگاه برای شما یافت نشد");

            var closure = new GymClosure
            {
                GymId = update.GymId,
                ClosureDate = update.ClosureDate.ToDateTime(TimeOnly.MinValue),
                DayOfWeek = update.ClosureDate.DayOfWeek,
                IsAllDay = update.IsAllDay,
                From = update.From,
                To = update.To,
                Reason = update.Reason,
                GymOwnerPublicKey = whois
            };

            await _gymClosureRepository.InsertOneAsync(closure);

            return MapToResultDto(closure);
        }

        public async Task<GymClosureResult> UpdateAsync(GymClosureEditUpdate dto, string whois)
        {
            var closure = await _gymClosureRepository.FindOneAsync(c => c.GymClosureId == dto.GymClosureId && c.GymOwnerPublicKey == whois);
            if (closure == null)
                throw new NotFoundException("باشگاه مورد نظر برای ویرایش تعطیلی پیدا نشد.");

            if (await HasTimeConflictAsync(dto.GymId, dto.ClosureDate.ToDateTime(TimeOnly.MinValue), dto.From, dto.To, dto.GymClosureId))
                throw new ValidationException("خطا: در این تاریخ و ساعت، تعطیلی دیگری ثبت شده است.");


            closure.GymId = dto.GymId;
            closure.ClosureDate = dto.ClosureDate.ToDateTime(TimeOnly.MinValue);
            closure.DayOfWeek = dto.ClosureDate.DayOfWeek;
            closure.IsAllDay = dto.IsAllDay;
            closure.From = dto.From;
            closure.To = dto.To;
            closure.Reason = dto.Reason;
            closure.ModifiedBy = whois;
            closure.ModifiedMoment = DateTime.UtcNow;

            await _gymClosureRepository.ReplaceOneAsync(closure);

            return MapToResultDto(closure);
        }

        public async Task<GymClosureResult> DeleteAsync(RemoveGymClosureUpdate update, string whois)
        {
            var closure = await _gymClosureRepository.FindOneAsync(c => c.GymClosureId == update.GymClosureId && c.GymOwnerPublicKey == whois);
            if (closure == null)
                throw new Exception("باشگاه برای شما یافت نشد");

            await _gymClosureRepository.DeleteOneAsync(q => q.GymClosureId == update.GymClosureId);

            return MapToResultDto(closure);
        }

        public async Task<GymClosureResult> GetOneByIdAsync(GymClosureIdUpdate update)
        {
            var closure = await _gymClosureRepository.FindOneAsync(c => c.GymClosureId == update.GymClosureId) ??
                throw new NotFoundException("اطلاعات تعطیلی یافت نشد");
            return MapToResultDto(closure);
        }

        public async Task<GymClosureListResult> GetListForGymOwnerAsync(GymClosureListUpdate update, string whois)
        {
            var result = new GymClosureListResult();

            // Pagination
            var page = update.Pagination?.Page <= 0 ? 1 : update.Pagination.Page;
            var size = update.Pagination?.Size ?? 25;
            var skip = (page - 1) * size;

            var filterBuilder = Builders<GymClosure>.Filter;
            var filter = filterBuilder.Eq(c => c.GymId, update.GymId) &
                         filterBuilder.Eq(c => c.GymOwnerPublicKey, whois) &
                         filterBuilder.Eq(c => c.IsDeleted, false);

            if (update.From.HasValue)
            {
                var fromDate = update.From.Value.ToDateTime(TimeOnly.MinValue);
                filter &= filterBuilder.Gte(c => c.ClosureDate, fromDate);
            }

            if (update.To.HasValue)
            {
                var toDate = update.To.Value.ToDateTime(TimeOnly.MaxValue);
                filter &= filterBuilder.Lte(c => c.ClosureDate, toDate);
            }

            var totalCount = await _gymClosureRepository.CountAsync(c => filter.Inject());
            result.TotalCount = (int)totalCount;
            result.PageCount = (int)Math.Ceiling(totalCount / (double)size);

            if (totalCount == 0)
                return result;

            var closures = await _gymClosureRepository
                .Find(filter)
                .SortBy(c => c.ClosureDate)
                .Skip(skip)
                .Limit(size)
                .ToListAsync();

            result.Data = closures.Select(MapToResultDto).ToList();

            return result;
        }

        #endregion



        #region Admin

        public async Task<GymClosureForAdminResult> CreateByAdminAsync(GymClosureCreateForAdminUpdate dto)
        {
            var result = await CreateAsync(
                new GymClosureCreateUpdate
                {
                    GymId = dto.GymId,
                    ClosureDate = dto.ClosureDate,
                    IsAllDay = dto.IsAllDay,
                    From = dto.From,
                    To = dto.To,
                    Reason = dto.Reason
                },
                dto.GymOwnerPublicKey
            );

            return MapToAdminResult(result, dto.GymOwnerPublicKey);
        }

        public async Task<GymClosureForAdminResult> UpdateByAdminAsync(GymClosureEditForAdminUpdate dto)
        {
            var result = await UpdateAsync(
                new GymClosureEditUpdate
                {
                    GymClosureId = dto.GymClosureId,
                    GymId = dto.GymId,
                    ClosureDate = dto.ClosureDate,
                    IsAllDay = dto.IsAllDay,
                    From = dto.From,
                    To = dto.To,
                    Reason = dto.Reason
                },
                dto.GymOwnerPublicKey
            );

            return MapToAdminResult(result, dto.GymOwnerPublicKey);
        }

        public async Task<GymClosureForAdminResult> DeleteByAdminAsync(RemoveGymClosureForAdminUpdate dto)
        {
            var result = await DeleteAsync(
                new RemoveGymClosureUpdate { GymClosureId = dto.GymClosureId },
                dto.GymOwnerPublicKey
            );

            return MapToAdminResult(result, dto.GymOwnerPublicKey);
        }

        public async Task<GymClosureForAdminResult> GetOneByIdByAdminAsync(GymClosureIdUpdate update)
        {
            var closure = await _gymClosureRepository.FindOneAsync(c => c.GymClosureId == update.GymClosureId) ??
               throw new NotFoundException("اطلاعات تعطیلی یافت نشد");
            return MapToAdminResult(closure);
        }
       
        public async Task<MonjoFilteredResult<GymClosureForAdminResult>> GetListForAdminAsync(MonjoQuery query)
        {
            try
            {
                query.WithBase<GymClosureForAdminResult>();

                var data = await _gymClosureRepository.AsQueryable()
                   .Apply(query.Where)
                   .Apply(query.Order)
                   .Select(result => new GymClosureForAdminResult
                   {
                       GymClosureId = result.GymClosureId,
                       GymId = result.GymId,
                       ClosureDate = result.ClosureDate,
                       DayOfWeek = result.DayOfWeek,
                       IsAllDay = result.IsAllDay,
                       From = result.From,
                       To = result.To,
                       Reason = result.Reason,
                       CreatedMoment = result.CreatedMoment,
                       ModifiedMoment = result.ModifiedMoment,
                       GymOwnerPublicKey = result.GymOwnerPublicKey
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




        #region Helpers

        private GymClosureForAdminResult MapToAdminResult(GymClosureResult result, string gymOwnerPublicKey)
        {
            return new GymClosureForAdminResult
            {
                GymClosureId = result.GymClosureId,
                GymId = result.GymId,
                ClosureDate = result.ClosureDate,
                DayOfWeek = result.DayOfWeek,
                IsAllDay = result.IsAllDay,
                From = result.From,
                To = result.To,
                Reason = result.Reason,
                CreatedMoment = result.CreatedMoment,
                ModifiedMoment = result.ModifiedMoment,
                GymOwnerPublicKey = gymOwnerPublicKey
            };
        }

        private GymClosureForAdminResult MapToAdminResult(GymClosure result)
        {
            return new GymClosureForAdminResult
            {
                GymClosureId = result.GymClosureId,
                GymId = result.GymId,
                ClosureDate = result.ClosureDate,
                DayOfWeek = result.DayOfWeek,
                IsAllDay = result.IsAllDay,
                From = result.From,
                To = result.To,
                Reason = result.Reason,
                CreatedMoment = result.CreatedMoment,
                ModifiedMoment = result.ModifiedMoment,
                GymOwnerPublicKey = result.GymOwnerPublicKey
            };
        }


        private async Task<bool> HasTimeConflictAsync(
            string gymId,
            DateTime closureDate,
            long? from,
            long? to,
            string excludeClosureId = null)
        {
            var filterBuilder = Builders<GymClosure>.Filter;

            var filter = filterBuilder.Eq(c => c.GymId, gymId) &
                         filterBuilder.Eq(c => c.ClosureDate, closureDate);

            if (!string.IsNullOrEmpty(excludeClosureId))
            {
                filter &= filterBuilder.Ne(c => c.GymClosureId, excludeClosureId);
            }

            filter &= filterBuilder.Or(
                filterBuilder.Eq(c => c.IsAllDay, true),
                filterBuilder.And(
                    filterBuilder.Lte(c => c.From, to ?? long.MaxValue),
                    filterBuilder.Gte(c => c.To, from ?? long.MinValue)
                )
            );

            var count = await _gymClosureRepository.CountAsync(c => filter.Inject());

            return count > 0;
        }

        private GymClosureResult MapToResultDto(GymClosure closure)
        {
            return new GymClosureResult
            {
                GymClosureId = closure.GymClosureId,
                GymId = closure.GymId,
                ClosureDate = closure.ClosureDate,
                DayOfWeek = closure.DayOfWeek,
                IsAllDay = closure.IsAllDay,
                From = closure.From,
                To = closure.To,
                Reason = closure.Reason,
                CreatedMoment = closure.CreatedMoment,
                ModifiedMoment = closure.ModifiedMoment
            };
        }

        #endregion

    }
}
