using XFit.Utilities.Attributes;

namespace XFit.Services._Deposit.DTOs
{
    public class CreateDepositUpdate
    {
        [NumericInputValidation]public decimal Amount { get; set; }
    } 
}
