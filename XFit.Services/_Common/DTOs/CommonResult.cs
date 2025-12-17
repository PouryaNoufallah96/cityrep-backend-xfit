namespace XFit.Services._Common.DTOs
{
    public class CommonResult
    {
        public DateTime CreatedMoment { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedMoment { get; set; } = null;
    }
}
