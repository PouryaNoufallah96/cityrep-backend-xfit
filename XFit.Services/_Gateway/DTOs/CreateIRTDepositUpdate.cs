using System.ComponentModel.DataAnnotations;

namespace XFit.Services._Gateway.DTOs
{
    public class CreateIRTDepositUpdate
    { 
        [Required]
        public decimal Amount { get; set; }

        public string Description { get; set; }
    }
}
