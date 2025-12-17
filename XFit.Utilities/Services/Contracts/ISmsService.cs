namespace XFit.Utilities.Services.Contracts
{
    public interface ISmsService
    {
        Task SendTextMessageAsync(string mobileNumber, string message);
        Task SendVerificationMessageAsync(string mobileNumber, string code);
        Task SendVerificationMessageAsync(string mobileNumber, string templateName, params string[] data);
    }
}
