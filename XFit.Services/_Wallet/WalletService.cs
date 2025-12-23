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
        IDepositRepository _depositRepository,
        IWithdrawalRepository _withdrawalRepository,
        IGymAttendanceRepository _gymAttendanceRepository) : IWalletService, IScopedDependency
    {
        public Task<WalletResult> GetOrCreateWalletAsync(string whois, string userRole)
        {
            if (userRole.ToLower() == "client")
                return GetClientWalletAsync(whois);

            else if (userRole.ToLower() == "gymowner")
                return GetGymOwnerWalletAsync(whois);

            else throw new BadRequestException("کاربر نامشخص");
        }

        private async Task<WalletResult> GetGymOwnerWalletAsync(string whois)
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

        private async Task<WalletResult> GetClientWalletAsync(string whois)
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
