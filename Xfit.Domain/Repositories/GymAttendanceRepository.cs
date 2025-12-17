using Xfit.Domain.Collections;
using Xfit.Domain.Repositories.Contracts;
using XFit.Utilities.MongoDatabase;
using XFit.Utilities.MongoDatabase.Contracts;
using static XFit.Utilities.Constants.RegisterMode;


namespace Xfit.Domain.Repositories
{
    public class GymAttendanceRepository(IMonjoConnection connection)
        : MonjoRepository<GymAttendance>(connection), IGymAttendanceRepository, ISingletonDependency
    {
    }
}