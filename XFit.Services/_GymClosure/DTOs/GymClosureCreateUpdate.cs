using XFit.Services._Common.DTOs;
using XFit.Utilities.Attributes;
using XFit.Utilities.DTOs;

namespace XFit.Services._GymClosure.DTOs
{

    #region GymOwner
    public class GymClosureCreateUpdate
    {
        [RequiredInput(customMessage: "شناسه باشگاه اجباری است")]
        public string GymId { get; set; }

        [RequiredInput(customMessage: "تاریخ تعطیلی اجباری است")]
        public DateOnly ClosureDate { get; set; }
        public bool IsAllDay { get; set; } = false;
        public long? From { get; set; }
        public long? To { get; set; }
        public string Reason { get; set; }
    }


    public class GymClosureEditUpdate : GymClosureCreateUpdate
    {
        public string GymClosureId { get; set; } = null;

    }

    public class GymClosureIdUpdate
    {
        [RequiredInput(customMessage: "شناسه  اجباری است")]
        public string GymClosureId { get; set; }
    }


    public class RemoveGymClosureUpdate : GymClosureIdUpdate
    {

    }

    public class GymClosureResult : CommonResult
    {
        public string GymClosureId { get; set; }
        public string GymId { get; set; }
        public DateTime ClosureDate { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public bool IsAllDay { get; set; }
        public long? From { get; set; }
        public long? To { get; set; }
        public string Reason { get; set; }
    }


    public class GymClosureListUpdate
    {
        public string GymId { get; set; }
        public Pagination Pagination { get; set; }
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }
    }

    public class GymClosureListResult
    {
        public List<GymClosureResult> Data { get; set; } = [];
        public int PageCount { get; set; } = 0;
        public int TotalCount { get; set; } = 0;
    }
    #endregion
}
