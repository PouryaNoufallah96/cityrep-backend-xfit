using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Xfit.Domain.Collections;
using Xfit.Domain.Repositories;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Gym;
using XFit.Services._GymTrend.DTOs;
using XFit.Utilities.Exceptions.Common;
using XFit.Utilities.MongoDatabase.Filter;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._GymTrend
{
    public class GymTrendService(IGymTrendRepository _gymTrendRepository, IGymService _gymService)
        : IGymTrendService, IScopedDependency
    {
        /// <summary>
        /// Create new gym trend
        /// </summary>
        public async Task<GymTrend> CreateAsync(CreateGymTrendUpdate update)
        {
            var title = update.Title.Trim();

            var titleExists = await _gymTrendRepository.ExistsAsync(x =>
                x.Title == title
            );

            if (titleExists)
                throw new Exception("Gym trend with this title already exists.");

            var entity = new GymTrend
            {
                Title = title,
                IconUrl = update.IconUrl
            };

            await _gymTrendRepository.InsertOneAsync(entity);
            return entity;
        }

        /// <summary>
        /// Edit gym trend
        /// </summary>
        public async Task<GymTrend> EditAsync(EditGymTrendUpdate update)
        {
            var title = update.Title.Trim();

            var gymTrend = await _gymTrendRepository.FindOneAsync(x =>
                x.GymTrendId == update.GymTrendId
            ) ?? throw new NotFoundException("Gym trend not found.");

            var titleExists = await _gymTrendRepository.ExistsAsync(x =>
                x.Title == title && x.GymTrendId != update.GymTrendId
            );

            if (titleExists)
                throw new BadRequestException("عنوان تکراری می‌باشد");

            bool isChanged = false;

            if (gymTrend.Title != title)
            {
                gymTrend.Title = title;
                isChanged = true;
            }

            if (gymTrend.IconUrl != update.IconUrl)
            {
                gymTrend.IconUrl = update.IconUrl;
                isChanged = true;
            }

            if (isChanged)
            {
                await _gymTrendRepository.ReplaceOneAsync(gymTrend);

                await _gymService.UpdateGymsWithTrendAsync(gymTrend);
            }

            return gymTrend;
        }


        

        /// <summary>
        /// Remove gym trend
        /// </summary>
        public async Task RemoveAsync(RemoveGymTrendUpdate update)
        {
            var gymTrend = await _gymTrendRepository.FindOneAsync(x =>
                x.GymTrendId == update.GymTrendId
            ) ?? throw new NotFoundException("Gym trend not found.");

            if (await _gymService.IsTrendUsedAsync(update.GymTrendId)) throw new BadRequestException("this trend is used in gyms");

            await _gymTrendRepository.DeleteOneAsync(x =>
                x.GymTrendId == update.GymTrendId
            );
        }

        /// <summary>
        /// Get gym trend by id
        /// </summary>
        public async Task<GymTrend> GetByIdAsync(GymTrendIdUpdate update)
        {
            var gymTrend = await _gymTrendRepository.FindOneAsync(x =>
                x.GymTrendId == update.GymTrendId
            ) ?? throw new NotFoundException("Gym trend not found.");

            return gymTrend;
        }

        /// <summary>
        /// Get all gym trends (filtered)
        /// </summary>
        public Task<MonjoFilteredResult<GymTrend>> GetAllAsync(MonjoQuery query)
        {
            return _gymTrendRepository.FilterByAsync(query);
        }

        public async Task<List<GymTrend>> GetAllAsync()
        {
            return await _gymTrendRepository.AsQueryable().Take(50).ToListAsync();
        }
    }
}
