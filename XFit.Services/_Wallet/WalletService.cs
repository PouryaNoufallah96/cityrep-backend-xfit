using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Xfit.Domain.Collections;
using Xfit.Domain.Common;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Wallet.DTOs;
using XFit.Utilities.Constants;
using XFit.Utilities.DTOs;
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


        /// <summary>
        /// use for create or get wallet for users in each role
        /// </summary>
        /// <param name="whois"></param>
        /// <param name="userRole"></param>
        /// <returns></returns>
        /// <exception cref="BadRequestException"></exception>
        public async Task<WalletResult> GetOrCreateWalletAsync(string whois, string userRole)
        {

            var wallet = await _walletRepository.AsQueryable()
                .FirstOrDefaultAsync(q => q.PublicKey == whois);

            if (wallet != null)
            {
                return new WalletResult
                {
                    WalletId = wallet.WalletId,
                    TotalBalance = wallet.TotalBalance,
                    AvailableBalance = wallet.AvailableBalance,
                    FrozenBalance = wallet.FrozenBalance
                };
            }
           
            //if (userRole.ToLower() == "client")
            //    return await SyncClientWalletAsync(whois);

            //else if (userRole.ToLower() == "gymowner")
            //    return await SyncGymOwnerWalletAsync(whois);

            else throw new BadRequestException("کاربر نامشخص");

        }


        /// <summary>
        /// use for make wallet should update true for queue
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public async Task MakeWalletShouldUpdateAsync(string publicKey)
        {
            var filter = Builders<Wallet>.Filter.Eq(q => q.PublicKey, publicKey);
            var update = Builders<Wallet>.Update.Set(q => q.ShouldUpdate, true);
            await _walletRepository.FindOneAndUpdateAsync(filter, update);
        }

        
        /// <summary>
        /// use for update should update wallets
        /// </summary>
        /// <param name="publicKeys"></param>
        /// <returns></returns>
        public async Task MakeWalletShouldUpdateAsync(List<string> publicKeys)
        {
            var filter = Builders<Wallet>.Filter.In(q => q.PublicKey, publicKeys);
            var update = Builders<Wallet>.Update.Set(q => q.ShouldUpdate, true);
            await _walletRepository.UpdateManyAsync(filter, update);
        }


        /// <summary>
        /// use for initialize wallet
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="userRole"></param>
        /// <returns></returns>
        /// <exception cref="BaseException"></exception>
        public async Task InitWalletAsync(string publicKey, UserRole userRole)
        {
            try
            {
                if (await _walletRepository.ExistsAsync(q => q.PublicKey == publicKey && q.Role == userRole)) return;

                var wallet = new Wallet
                {
                    PublicKey = publicKey,
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


        /// <summary>
        /// sync single wallet balance
        /// </summary>
        /// <returns></returns>
        public async Task SyncWalletAsync()
        {
            var wallet = await _walletRepository.AsQueryable()
                .Where(q => q.ShouldUpdate == true)
                .OrderByDescending(q => q.ModifiedMoment).ThenBy(q => q.CreatedMoment)
                .FirstOrDefaultAsync();

            if (wallet == null)
                return;

            var walletresult = await SyncWalletAsync(wallet);
            wallet.FrozenBalance = walletresult.FrozenBalance;
            wallet.AvailableBalance = walletresult.AvailableBalance;
            wallet.TotalBalance = walletresult.TotalBalance;    
            await _walletRepository.ReplaceOneAsync(wallet);
        }


        /// <summary>
        /// for get  client transactions
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="pagination"></param>
        /// <returns></returns>
        /// <exception cref="BadRequestException"></exception>
        public async Task<ClientTransactionListResult> GetClientTransactionsAsync(
        string publicKey,
        Pagination pagination)
        {
            if (CurrentRequestContext.Role.ToLower() != "client")
                throw new BadRequestException("کاربر نامشخص");

            var depositsQuery = _depositRepository.AsQueryable()
                .Where(x => x.SourcePublicKey == publicKey)
                .Select(x => new ClientTransactionResult
                {
                    CreatedMoment = x.CreatedMoment,
                    Title = "واریز به کیف پول",
                    Price = x.Amount,
                    Type = ClientTransactionType.Deposit
                });

            var attendancePaymentsQuery = _gymAttendanceRepository.AsQueryable()
                .Where(x => x.ClientPublicKey == publicKey)
                .Select(x => new ClientTransactionResult
                {
                    CreatedMoment = x.CreatedMoment,
                    Title = x.GymTitle,
                    Price = x.SessionPrice,
                    Type = ClientTransactionType.GymAttendancePayment
                });

            var allTransactionsQuery = depositsQuery
                .Concat(attendancePaymentsQuery)
                .OrderByDescending(x => x.CreatedMoment)
                .ThenByDescending(x => x.Type);

            var totalCount = await allTransactionsQuery.CountAsync();

            var data = await allTransactionsQuery
                .Skip((pagination.Page - 1) * pagination.Size)
                .Take(pagination.Size)
                .ToListAsync();

            return new ClientTransactionListResult
            {
                Data = data,
                TotalCount = totalCount,
                PageCount = (int)Math.Ceiling(totalCount / (double)pagination.Size)
            };
        }


        /// <summary>
        /// use for get gym owner transactions
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="pagination"></param>
        /// <returns></returns>
        /// <exception cref="BadRequestException"></exception>
        public async Task<GymOwnerTransactionListResult> GetGymOwnerTransactionsAsync(
        string publicKey,
        Pagination pagination)
        {
            if (CurrentRequestContext.Role.ToLower() != "gymowner")

                throw new BadRequestException("کاربر نامشخص");

            var withdrawalsQuery = _withdrawalRepository.AsQueryable()
                .Where(x => x.PublicKey == publicKey)
                .Select(x => new GymOwnerTransactionResult
                {
                    CreatedMoment = x.CreatedMoment,
                    Title = "برداشت از کیف پول",
                    Price = x.Amount,
                    Type = GymOwnerTransactionType.Withdarawal
                });

            var attendancePaymentsQuery = _gymAttendanceRepository.AsQueryable()
                .Where(x => x.GymOwnerPublicKey == publicKey)
                .Select(x => new GymOwnerTransactionResult
                {
                    CreatedMoment = x.CreatedMoment,
                    Title = "انتقال به کیف پول",
                    Price = x.SessionPrice,
                    Type = GymOwnerTransactionType.GymAttendancePayment
                });

            var allTransactionsQuery = withdrawalsQuery
                .Concat(attendancePaymentsQuery)
                .OrderByDescending(x => x.CreatedMoment)
                .ThenByDescending(x => x.Type);

            var totalCount = await allTransactionsQuery.CountAsync();

            var data = await allTransactionsQuery
                .Skip((pagination.Page - 1) * pagination.Size)
                .Take(pagination.Size)
                .ToListAsync();

            return new GymOwnerTransactionListResult
            {
                Data = data,
                TotalCount = totalCount,
                PageCount = (int)Math.Ceiling(totalCount / (double)pagination.Size)
            };
        }


        /// <summary>
        /// specify wallet for sync by role
        /// </summary>
        /// <param name="wallet"></param>
        /// <returns></returns>
        private async Task<WalletResult> SyncWalletAsync(Wallet wallet)
        {
            if (wallet.Role == UserRole.Client)
               return await SyncClientWalletAsync(wallet.PublicKey);
            else
               return await SyncGymOwnerWalletAsync(wallet.PublicKey);

        } 


        /// <summary>
        /// use for update gym owner wallet
        /// </summary>
        /// <param name="whois"></param>
        /// <returns></returns>
        private async Task<WalletResult> SyncGymOwnerWalletAsync(string whois)
        {
            var totalIncome = await _gymAttendanceRepository.AsQueryable()
                .Where(q =>
                    q.GymOwnerPublicKey == whois &&
                    (q.GymAttendanceState == GymAttendanceState.Used ||
                     q.GymAttendanceState == GymAttendanceState.NoShow))
                .SumAsync(q => q.SessionPrice);

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


        /// <summary>
        /// use for update clinet wallet
        /// </summary>
        /// <param name="whois"></param>
        /// <returns></returns>
        private async Task<WalletResult> SyncClientWalletAsync(string whois)
        {
            var clientDeposits = await _depositRepository.AsQueryable()
             .Where(q => q.SourcePublicKey == whois && q.State == Xfit.Domain.Collections.DepositState.Done)
             .SumAsync(q => q.Amount);

            var clientAttendance = await _gymAttendanceRepository.AsQueryable()
                .Where(q => q.ClientPublicKey == whois &&
                    q.GymAttendanceState != GymAttendanceState.Pending &&
                    q.GymAttendanceState != GymAttendanceState.Failed)
                .SumAsync(q => q.SessionPrice);

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
