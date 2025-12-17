using XFit.Utilities.Attributes;

namespace XFit.Services._GymTrend.DTOs
{
    public class CreateGymTrendUpdate
    {
        [StringInputValidation(maxLength: 250)]
        public string Title { get; set; }

        [StringInputValidation(maxLength: 500)]
        public string IconUrl { get; set; }
    }

    public class EditGymTrendUpdate
    {
        [StringInputValidation(maxLength: 250)]
        public string GymTrendId { get; set; }

        [StringInputValidation(maxLength: 250)]
        public string Title { get; set; }

        [StringInputValidation(maxLength: 500)]
        public string IconUrl { get; set; }
    }

    public class RemoveGymTrendUpdate
    {
        [StringInputValidation(maxLength: 250)]
        public string GymTrendId { get; set; }
    }

    public class GymTrendIdUpdate
    {
        [StringInputValidation(maxLength: 250)]
        public string GymTrendId { get; set; }
    }
}
