using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Xfit.Domain.Collections;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Deposit.DTOs;
using XFit.Services._Gateway;
using XFit.Services._Gym;
using XFit.Utilities.Constants;
using XFit.Utilities.Exceptions.Common;
using XFit.Utilities.MongoDatabase.Filter;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._Deposit
{
    public class DepositService(IDepositRepository _depositRepository,IGatewayService _gatewayService , IGymService _gymService) : IDepositService, IScopedDependency
    {
        public async Task<string> CreateDepositAsync(CreateDepositUpdate update,string whois)
        {

            if (update.Amount < 10000 || update.Amount > 100000000) throw new BadRequestException("مقدار واریز معتبر نیست");
          
            var depositReference = "Done for test";//await _gatewayService.CreateZarinPalDepositAsync()
            var newDeposit = new Deposit
            {
                Amount = update.Amount,
                Role = Xfit.Domain.Common.UserRole.Client,
                //State = DepositState.Pending,
                State = DepositState.Done ,
                SourceFullName = CurrentRequestContext.FullName,
                SourcePublicKey = whois,
                DepositReference = depositReference,
            };

            await _depositRepository.InsertOneAsync(newDeposit);
            return depositReference;
        }


        public async Task<DepositResult> VerifyDepositAsync(VerifyDepositUpdate update)
        {
            var deposit = await _depositRepository.FindOneAsync(q =>
            q.DepositReference == update.DepositReference &&
            q.State == DepositState.Pending) ??
                throw new NotFoundException("واریز درحال انتظار یافت نشد");

            try
            {
                var verifyResult = await _gatewayService.VerifyZarinPalDepositAsync(deposit.DepositReference, deposit.Amount);

                var depositFilter = Builders<Deposit>.Filter.Eq(d => d.Id, deposit.Id);
                var depositUpdate = Builders<Deposit>.Update.Set(d => d.State, DepositState.Done);
                await _depositRepository.FindOneAndUpdateAsync(depositFilter, depositUpdate);

               
                return new DepositResult
                {
                    State = DepositState.Done,
                    Amount = deposit.Amount,
                    Reference = update.DepositReference
                };
            }
            catch (Exception)
            {
                var depositFilter = Builders<Deposit>.Filter.Eq(d => d.Id, deposit.Id);
                var depositUpdate = Builders<Deposit>.Update.Set(d => d.State, DepositState.Failed);
                await _depositRepository.FindOneAndUpdateAsync(depositFilter, depositUpdate);

                return new DepositResult
                {
                    State = DepositState.Failed,
                    Amount = deposit.Amount,
                    Reference = update.DepositReference

                };
            }
        }

        public async Task ProcessForPendingDepositsAsync()
        {
            var deposit = await _depositRepository.AsQueryable()
                .Where(q => q.State == DepositState.Pending && q.CreatedMoment <= DateTime.UtcNow.AddMinutes(-11))
                .OrderBy(q => q.CreatedMoment)
                .FirstOrDefaultAsync();

            if (deposit != null)
            {
                await VerifyDepositAsync(new VerifyDepositUpdate { DepositReference = deposit.DepositReference });
            }
        }


        public async Task<MonjoFilteredResult<Deposit>> GetAllDepositsAsync(MonjoQuery monjoQuery)
        {
            return await _depositRepository.FilterByAsync(monjoQuery);
        }


    }
}
