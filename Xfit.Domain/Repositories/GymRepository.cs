using MongoDB.Bson;
using MongoDB.Driver;
using Xfit.Domain.Collections;
using Xfit.Domain.Repositories.Contracts;
using XFit.Utilities.MongoDatabase;
using XFit.Utilities.MongoDatabase.Contracts;
using static XFit.Utilities.Constants.RegisterMode;

namespace Xfit.Domain.Repositories
{
    public class GymRepository(IMonjoConnection connection)
    : MonjoRepository<Gym>(connection), IGymRepository, ISingletonDependency
    {
        protected override void Configure()
        {
            if (CollectionExists())
                return;

            CreateGeo2DSphereIndex("Address.Location", "idx_address_location_2dsphere");
            AscendingIndex(x => x.State).Build();
            AscendingIndex(x => x.Level).Build();
        }
    }
}
