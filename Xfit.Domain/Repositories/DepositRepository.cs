using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xfit.Domain.Collections;
using Xfit.Domain.Repositories.Contracts;
using XFit.Utilities.MongoDatabase;
using XFit.Utilities.MongoDatabase.Contracts;
using static XFit.Utilities.Constants.RegisterMode;

namespace Xfit.Domain.Repositories
{
    public class DepositRepository(IMonjoConnection connection)
     : MonjoRepository<Deposit>(connection), IDepositRepository, ISingletonDependency
    {
    }
}
