using Xfit.Domain.Collections;
using Xfit.Domain.Common;
using XFit.Services._Common.DTOs;

namespace XFit.Services._Gym.DTOs.Results
{
    public class GymResult : CommonResult
    {
        public string GymId { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public GymLevel Level { get; set; }

        public List<Gender> SupportedGender { get; set; } 

        public AddressInfo Address { get; set; }
        public List<GymTrendWorkingHour> GymTotalWorkingHour { get; set; }

        public Contact Contact { get; set; } = null;
        public List<GymImage> Images { get; set; } = null;
        public List<GymTrendInfo> Trends { get; set; } = null;
        public List<GymFacilityRef> Facilities { get; set; } = null;
        public GymState State { get; set; }


        public decimal Rate { get; set; }
    }

    public class GymListResult
    {
        public List<GymResult> Data { get; set; } = [];
        public int PageCount { get; set; } = 0;
        public int TotalCount { get; set; } = 0;
    }



}
