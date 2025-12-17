using XFit.Utilities.Attributes;

namespace XFit.Services._Admin.DTOs
{
    public record LoginUpdate
    {
        [StringInputValidation(true)] public string UserName { get; set; }
        [StringInputValidation(true, maxLength: 100)] public string Password { get; set; }

        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}
