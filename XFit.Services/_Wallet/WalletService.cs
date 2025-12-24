using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Xfit.Domain.Collections;
using Xfit.Domain.Common;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Wallet.DTOs;
using XFit.Utilities.Exceptions.Common;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._Wallet
{
    public class WalletService(
        IWalletRepository _walletRepository,
        IDepositRepository _depositRepository,
        IWithdrawalRepository _withdrawalRepository,
        IGymAttendanceRepository _gymAttendanceRepository) : IWalletService, IScopedDependency
    {
        public async Task<WalletResult> GetOrCreateWalletAsync(string whois, string userRole)
        {

            var wallet = await _walletRepository.AsQueryable()
                .FirstOrDefaultAsync(q => q.WalletId == whois);

            if (wallet != null)
                return new WalletResult
                {
                    WalletId = wallet.WalletId,
                    TotalBalance = wallet.TotalBalance,
                    AvailableBalance = wallet.AvailableBalance,
                    FrozenBalance = wallet.FrozenBalance
                };


            if (userRole.ToLower() == "client")
                return await SyncClientWalletAsync(whois);

            else if (userRole.ToLower() == "gymowner")
                return await SyncGymOwnerWalletAsync(whois);

            else throw new BadRequestException("کاربر نامشخص");

        }

        public async Task MakeWalletShouldUpdateAsync(string publicKey)
        { 
            var filter = Builders<Wallet>.Filter.Eq(q => q.WalletId, publicKey);
            var update = Builders<Wallet>.Update.Set(q => q.ShouldUpdate, true);
            await _walletRepository.FindOneAndUpdateAsync(filter, update);
        }

        public async Task MakeWalletShouldUpdateAsync(List<string> publicKeys)
        { 
            var filter = Builders<Wallet>.Filter.In(q => q.WalletId, publicKeys);
            var update = Builders<Wallet>.Update.Set(q => q.ShouldUpdate, true);
            await _walletRepository.UpdateManyAsync(filter, update);
        }

        public async Task InitWalletAsync(string publicKey , UserRole userRole)
        {
            try
            {
                if (await _walletRepository.ExistsAsync(q => q.PublicKey == publicKey && q.Role == userRole)) return;

                var wallet = new Wallet
                {
                    WalletId = publicKey,
                    TotalBalance = 0m,
                    AvailableBalance = 0m,
                    FrozenBalance = 0m,
                    Role = userRole,
                    ShouldUpdate = false
                };

                await _walletRepository.InsertOneAsync(wallet);
            }
            catch (Exception)
            {
                throw new BaseException();
            }
                
        }

        public async Task SyncWalletAsync()
        {
            var wallet = await  _walletRepository.AsQueryable()
                .Where(q => q.ShouldUpdate == true)
                .OrderByDescending(q => q.ModifiedMoment)
                .FirstOrDefaultAsync();

            if( wallet == null)
                return;
            await SyncWalletAsync(wallet);

        }


        private async Task SyncWalletAsync(Wallet wallet)
        {
            if (wallet.Role == UserRole.Client)
                await SyncClientWalletAsync(wallet.WalletId);
            else
                await SyncGymOwnerWalletAsync(wallet.WalletId);
         
        }

        private async Task<WalletResult> SyncGymOwnerWalletAsync(string whois)
        {
            var totalIncome = await _gymAttendanceRepository.AsQueryable()
                .Where(q =>
                    q.GymOwnerPublicKey == whois &&
                    q.PaymentState == GymAttendanceState.Paid)
                .SumAsync(q => q.Price);

            var withdrawalSums = await _withdrawalRepository.AsQueryable()
                .Where(q =>
                    q.PublicKey == whois &&
                    (q.State == WithdrawalState.Done || q.State == WithdrawalState.Pending))
                .GroupBy(q => q.State)
                .Select(g => new
                {
                    State = g.Key,
                    Total = g.Sum(x => x.Amount)
                })
                .ToListAsync();

            var doneWithdrawals = withdrawalSums
                .FirstOrDefault(x => x.State == WithdrawalState.Done)?.Total ?? 0m;

            var pendingWithdrawals = withdrawalSums
                .FirstOrDefault(x => x.State == WithdrawalState.Pending)?.Total ?? 0m;

            var totalBalance = totalIncome - doneWithdrawals;
            var availableBalance = totalBalance - pendingWithdrawals;

            return new WalletResult
            {
                WalletId = whois,
                TotalBalance = totalBalance,
                AvailableBalance = availableBalance < 0 ? 0 : availableBalance,
                FrozenBalance = pendingWithdrawals
            };
        }

        private async Task<WalletResult> SyncClientWalletAsync(string whois)
        {
            var clientDeposits = await _depositRepository.AsQueryable()
             .Where(q => q.SourcePublicKey == whois && q.State == Xfit.Domain.Collections.DepositState.Done)
             .SumAsync(q => q.Amount);

            var clientAttendance = await _gymAttendanceRepository.AsQueryable()
                .Where(q => q.ClientPublicKey == whois && q.PaymentState == GymAttendanceState.Paid).SumAsync(q => q.Price);


            var balance = clientDeposits - clientAttendance;

            return new WalletResult
            {
                WalletId = whois,
                TotalBalance = balance,
                AvailableBalance = balance,
                FrozenBalance = 0m
            };
        }

      
    }
}
