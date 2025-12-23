using Xfit.Domain.Collections;
using Xfit.Domain.Common;
using XFit.Services._Common.DTOs;

namespace XFit.Services._GymAttendance.DTOs
{
    public class GetGymOwnerGymAttendanceListResult
    {
        public List<GetGymOwnerGymAttendanceResult> Data { get; set; } = [];
        public int PageCount { get; set; } = 0;
        public int TotalCount { get; set; } = 0;
    }


    public class GetGymOwnerGymAttendanceResult : CommonResult
    {  
        public string GymAttendanceId { get; set; }
        public string GymAttendanceReference { get; set; }

        public string GymId { get; set; }
        public string GymTitle { get; set; }
        public string GymTrendId { get; set; }
        public string GymTrendTitle { get; set; }
        public string GymOwnerPublicKey { get; set; }

        public string Notes { get; set; }
        public GymLevel Level { get; set; }
        public decimal Price { get; set; }

        public DateTime? ExpirePaymentCode { get; set; }
        public GymAttendanceState PaymentState { get; set; }
        public DateTime? PaymentMoment { get; set; }

        public decimal? GivenRate { get; set; } = null;
    }
}