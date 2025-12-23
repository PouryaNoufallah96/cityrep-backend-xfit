using Xfit.Domain.Collections;
using Xfit.Domain.Common;
using XFit.Services._Common.DTOs;

namespace XFit.Services._GymAttendance.DTOs
{
    public class CreateGymAttendanceUpdate
    {
        public string GymId { get; set; }
        public string GymTrendId { get; set; }

    }


    public class GymAttendanceResult : CommonResult
    {
        public string GymAttendanceId { get; set; } 

        public string GymId { get; set; }
        public string GymTitle { get; set; }
        public string GymTrendId { get; set; }
        public string GymTrendTitle { get; set; }


        public string Notes { get; set; }
        public GymLevel Level { get; set; }
        public decimal Price { get; set; }

        public DateTime? ExpirePaymentCode { get; set; }
        public GymAttendance PaymentState { get; set; }
         
        public decimal GivenRate { get; set; }

    }

}
