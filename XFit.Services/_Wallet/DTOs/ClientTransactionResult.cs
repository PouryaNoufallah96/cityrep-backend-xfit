namespace XFit.Services._Wallet.DTOs
{

    public class ClientTransactionListResult
    {
        public List<ClientTransactionResult> Data { get; set; } = [];
        public int PageCount { get; set; } = 0;
        public int TotalCount { get; set; } = 0;
    }   


    public class ClientTransactionResult
    {
        public DateTime CreatedMoment { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public ClientTransactionType Type { get; set; }
    }


    public enum ClientTransactionType
    {
        Deposit,
        GymAttendancePayment
    }   


    public class GymOwnerTransactionListResult
    {
        public List<GymOwnerTransactionResult> Data { get; set; } = [];
        public int PageCount { get; set; } = 0;
        public int TotalCount { get; set; } = 0;
    }   


    public class GymOwnerTransactionResult
    {
        public DateTime CreatedMoment { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public GymOwnerTransactionType Type { get; set; }
    }


    public enum GymOwnerTransactionType
    {
        Withdarawal,
        GymAttendancePayment 
    }   
}
