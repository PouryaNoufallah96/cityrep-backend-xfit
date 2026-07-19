using Xfit.Domain.Common;

namespace XFit.Services._Gym.DTOs.Results
{
    public class GymSessionPriceBandResult
    {
        public GymLevel Level { get; set; }
        public decimal FromPrice { get; set; }
        public decimal ToPrice { get; set; }
    }
}
