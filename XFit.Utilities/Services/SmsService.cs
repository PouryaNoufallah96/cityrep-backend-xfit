using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XFit.Utilities.Models.Settings;
using XFit.Utilities.Services.Contracts;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Utilities.Services
{
    public class GhasedakSMSService(GhasedakConnectionSetting _settings) : ISmsService, ISingletonDependency
    {
        private static readonly HttpClient _httpClient = new();

        public async Task SendTextMessageAsync(string mobileNumber, string message)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);

            var content = new
            {
                receptor = mobileNumber,
                message = message
            };

            var response = await _httpClient.PostAsJsonAsync(
                "https://api.ghasedaksms.com/v2/sms/send/simple", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"SMS send failed. StatusCode: {response.StatusCode}, Error: {error}");
            }
        }

        public async Task SendVerificationMessageAsync(string mobileNumber, string code)
        {
            await SendVerificationMessageAsync(mobileNumber, _settings.VerificationTemplateName, code);
        }

        public async Task SendVerificationMessageAsync(string mobileNumber, string templateName, params string[] data)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);

            using var formData = new MultipartFormDataContent
            {
                { new StringContent(mobileNumber), "receptor" },
                { new StringContent("1"), "type" }, // 1 = text message, 2 = voice message
                { new StringContent(templateName), "template" }
            };

            for (int i = 0; i < data.Length; i++)
            {
                formData.Add(new StringContent(data[i]), $"param{i + 1}");
            }

            var response = await _httpClient.PostAsync("https://api.ghasedaksms.com/v2/send/verify", formData);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Verification SMS send failed");

            try
            {
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var statusCode = result.GetProperty("result").GetProperty("code").GetInt32();
                var message = result.GetProperty("result").GetProperty("message").GetString();

                if (statusCode < 1000)
                    throw new Exception($"Ghasedak API returned warning or error: {message} (Code: {statusCode})");
            }
            catch (JsonException jsonEx)
            {
                throw new Exception($"Failed to parse Ghasedak response. Raw content: {responseContent}", jsonEx);
            }
        }
    }
}
