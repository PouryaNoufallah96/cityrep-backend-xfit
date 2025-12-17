namespace XFit.Services._Admin.DTOs
{
    public record ResetPasswordUpdate
    {
        public required string PublicKey { get; set; }
        public required string Password { get; set; }
    }
}
