using MongoDB.Bson;
using MongoDB.Driver;
using Xfit.Domain.Collections;
using Xfit.Domain.Repositories;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Gym;
using XFit.Services._GymFacility.DTOs;
using XFit.Utilities.Exceptions.Common;
using XFit.Utilities.MongoDatabase.Filter;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._GymFacility
{
    public class GymFacilityService(IGymFacilityRepository _gymFacilityRepository, IGymService _gymService) : IGymFacilityService, IScopedDependency
    {

        /// <summary>
        /// this method creates a new gym facility
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<GymFacility> CreateAsync(CreateGymFacilityUpdate update)
        {

            var title = update.Title.Trim();

            var titleExists = await _gymFacilityRepository.ExistsAsync(x =>
                x.Title == title
            );

            if (titleExists)
                throw new NotFoundException("Gym facility with this title already exists.");

            var entity = new GymFacility
            {
                Title = title
            };

            await _gymFacilityRepository.InsertOneAsync(entity);
            return entity;
        }

        public async Task<GymFacility> EditAsync(EditGymFacilityUpdate update)
        {
            var title = update.Title.Trim();

            var gymFacility = await _gymFacilityRepository.FindOneAsync(q => q.FacilityId == update.GymFacilityId)
                ?? throw new NotFoundException("Gym facility not found.");

            var titleExists = await _gymFacilityRepository.ExistsAsync(x =>
                x.Title == title && x.FacilityId != update.GymFacilityId
            );

            if (titleExists)
                throw new Exception("Gym facility with this title already exists.");

            bool isChanged = false;

            if (gymFacility.Title != title)
            {
                gymFacility.Title = title;
                isChanged = true;
            }

            if (isChanged)
            {
                await _gymFacilityRepository.ReplaceOneAsync(gymFacility);
                await _gymService.UpdateGymsWithFacilityAsync(gymFacility);
            }

            return gymFacility;
        }

        


        public async Task RemoveAsync(RemoveGymFacilityUpdate update)
        {

            var gymFactility = await _gymFacilityRepository.FindOneAsync(q => q.FacilityId == update.GymFacilityId)
                ?? throw new NotFoundException("Gym facility not found.");


            if (gymFactility == null)
                throw new Exception("Gym facility not found.");

            if (await _gymService.IsFacilityUsedAsync(update.GymFacilityId)) throw new BadRequestException("خدمات مورد نظر در باشگاه ها درحال استفاده است.");

            await _gymFacilityRepository.DeleteOneAsync(q => q.FacilityId == update.GymFacilityId);
        }

        public async Task<GymFacility> GetByIdAsync(GymFacilityIdUpdate update)
        {
            var gymFactility = await _gymFacilityRepository.FindOneAsync(q => q.FacilityId == update.GymFacilityId)
                ?? throw new NotFoundException("Gym facility not found.");

            return gymFactility;
        }

        public Task<MonjoFilteredResult<GymFacility>> GetAllAsync(MonjoQuery query)
        {
            return _gymFacilityRepository.FilterByAsync(query);
        }



    }
}
