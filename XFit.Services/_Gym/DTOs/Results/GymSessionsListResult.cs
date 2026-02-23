using Xfit.Domain.Collections;
using Xfit.Domain.Common;

namespace XFit.Services._Gym.DTOs.Results
{
    public class GymSessionsListResult
    {
        public List<GymSessionResult> Data { get; set; } = [];
        public int PageCount { get; set; } = 0;
        public int TotalCount { get; set; } = 0;
    }

    public class GymSessionResult
    {
        public string GymId { get; set; }
        public string GymTitle { get; set; }
        public string GymTrendId { get; set; }
        public string GymTrendName { get; set; }
        public string GymSessionId { get; set; }
        public DayOfWeek DayOfWeek { get; set; } 
        public decimal Price { get; set; }
        public GymTimeType TimeType { get; set; }
        public long From { get; set; }
        public long To { get; set; }
        public int? Capacity { get; set; } = null;
        public int? UsedCapacity { get; set; }
        public bool IsActive { get; set; }
        public Gender Gender { get; set; }
    }

}
 