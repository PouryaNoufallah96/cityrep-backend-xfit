using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Xfit.Domain.Collections;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Deposit.DTOs;
using XFit.Services._Deposit.DTOs.Settings;
using XFit.Services._Gym;
using XFit.Services._Wallet;
using XFit.Utilities.Constants;
using XFit.Utilities.Exceptions;
using XFit.Utilities.Exceptions.Common;
using XFit.Utilities.MongoDatabase.Filter;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._Deposit
{
    public class DepositService(IDepositRepository _depositRepository, IGymService _gymService,IWalletService _walletService, MockPaymentSettings _mockPaymentSettings) : IDepositService, IScopedDependency
    {
        private const decimal MinimumTopUpAmount = 10000;
        private const decimal MaximumDepositAmount = 100000000;

        public Task<string> CreateDepositAsync(CreateDepositUpdate update,string whois)
        {
            EnsurePaymentServiceIsAvailable();

            if (update.Amount < MinimumTopUpAmount || update.Amount > MaximumDepositAmount) throw new BadRequestException(ExceptionMessages.InvalidDepositAmount);

            return CreatePendingDepositAsync(update.Amount, whois);
        }

        public Task<string> CreateBookingDepositAsync(decimal amount, string whois)
        {
            EnsurePaymentServiceIsAvailable();

            if (amount <= 0 || amount > MaximumDepositAmount) throw new BadRequestException(ExceptionMessages.InvalidDepositAmount);

            return CreatePendingDepositAsync(amount, whois);
        }

        private async Task<string> CreatePendingDepositAsync(decimal amount, string whois)
        {
            var depositReference = Guid.NewGuid().ToString("N");
            var newDeposit = new Deposit
            {
                Amount = amount,
                Role = Xfit.Domain.Common.UserRole.Client,
                State = DepositState.Pending,
                SourceFullName = CurrentRequestContext.FullName,
                SourcePublicKey = whois,
                DepositReference = depositReference,
            };

            await _depositRepository.InsertOneAsync(newDeposit);
            await _walletService.MakeWalletShouldUpdateAsync(whois);
            return depositReference;
        }


        public async Task<DepositResult> VerifyDepositAsync(VerifyDepositUpdate update)
        {
            EnsurePaymentServiceIsAvailable();

            var deposit = await _depositRepository.FindOneAsync(q =>
            q.DepositReference == update.DepositReference &&
            q.State == DepositState.Pending) ??
                throw new NotFoundException("واریز درحال انتظار یافت نشد");

            try
            {
                if (update.MockOutcome == false)
                    throw new BadRequestException(ExceptionMessages.PaymentVerificationFailed);

                var depositFilter = Builders<Deposit>.Filter.Eq(d => d.Id, deposit.Id);
                var depositUpdate = Builders<Deposit>.Update.Set(d => d.State, DepositState.Done);
                await _depositRepository.FindOneAndUpdateAsync(depositFilter, depositUpdate);
                await _gymService.MakeDoneAttendanceAsync(update.DepositReference);


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
                await _gymService.UndoGymCapacityByAttendanceAsync(update.DepositReference);
                throw new BadRequestException(ExceptionMessages.PaymentVerificationFailed);
            }
        }

        public async Task ProcessForPendingDepositsAsync()
        {
            if (!_mockPaymentSettings.Enabled)
                return;

            var deposit = await _depositRepository.AsQueryable()
                .Where(q => q.State == DepositState.Pending && q.CreatedMoment <= DateTime.UtcNow.AddMinutes(-11))
                .OrderBy(q => q.CreatedMoment)
                .FirstOrDefaultAsync();

            if (deposit != null)
            {
                await VerifyDepositAsync(new VerifyDepositUpdate { DepositReference = deposit.DepositReference });
            }
        }

        private void EnsurePaymentServiceIsAvailable()
        {
            if (!_mockPaymentSettings.Enabled)
                throw new BadRequestException(ExceptionMessages.PaymentServiceUnavailable);
        }


        public async Task<MonjoFilteredResult<Deposit>> GetAllDepositsAsync(MonjoQuery monjoQuery)
        {
            return await _depositRepository.FilterByAsync(monjoQuery);
        }


    }
}
