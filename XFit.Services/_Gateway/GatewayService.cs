using System.Text.Json;
using XFit.Services._Gateway.DTOs;
using XFit.Utilities.Exceptions.Common;
using XFit.Utilities.Extension;
using XFit.Utilities.Services.Contracts;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Services._Gateway
{
    public class GatewayService(IRTHandlerSettings _settings, IRandomService _randomService) : IGatewayService , IScopedDependency
    {
        private readonly HttpClient httpClient = new HttpClient();




        #region create deposit

        public async Task<string> CreateZarinPalDepositAsync(string orderReference, CreateIRTDepositUpdate update)
        {
            var content =
                new
                {
                    merchant_id = _settings.ZarinPalMerchantId,
                    amount = (update.Amount * 10),
                    callback_url = _settings.WebCallbackUrl,
                    description = update.Description,
                    metadata = new { order_id = orderReference },
                };

            var result = await httpClient.PostAsJsonAsync<JsonElement>($"https://api.zarinpal.com/pg/v4/payment/request.json", content);
            try
            {
                if (result.GetProperty("data").GetProperty("code").GetInt32() != 100)
                    throw new Exception($"Error code: {result.GetProperty("data").GetProperty("code").GetInt32()}");

                var track = result.GetProperty("data").GetProperty("authority").GetString();
                return track;
                //return "https://www.zarinpal.com/pg/StartPay/" + cod;
            }
            catch (Exception e)
            {
                throw new BadRequestException("مبلغ پرداختی نباید بیشتر از 100 میلیون تومان باشد");
            }

        }

        #endregion



        #region Verify deposits

        public async Task<VerifyIRTDepositResult> VerifyZarinPalDepositAsync(string trackId, decimal amount)
        {
            var content = new
            {
                merchant_id = _settings.ZarinPalMerchantId,
                authority = trackId,
                amount = amount * 10
            };

            var result = await httpClient.PostAsJsonAsync<JsonElement>($"https://api.zarinpal.com/pg/v4/payment/verify.json", content);

            if (result.GetProperty("data").GetProperty("code").GetInt32() != 100 || result.GetProperty("data").GetProperty("message").GetString() != "Verified")
                throw new BadRequestException("پرداخت تایید نشده است");


            return new VerifyIRTDepositResult
            {
                DepositId = result.GetProperty("data").GetProperty("order_id").GetString(),
                //DepositId = result.GetProperty("data").GetProperty("ref_id").GetInt32().ToString(),
                Reference = result.GetProperty("data").GetProperty("ref_id").GetInt32().ToString()
            };
        }



        #endregion


        public async Task<decimal> GetFeeAsync(string networkName, decimal amount, string inputAddress = "")
        {
            decimal fee;
            if (amount <= 400000)
            {
                fee = amount * 0.01m;
            }
            else
            {
                var a = amount / 50000000m;

                fee = Math.Ceiling(a) * 4000m;
            }

            return fee * 2;
        }

        private string GetCallbackUrl(CreateIRTDepositUpdate update)
        {
            return "http://hayperdigital.com/checkout/result";
        }

    }
    public class IRTHandlerSettings : IHostedDependency
    {
        public string ZarinPalMerchantId { get; set; }
        public string WebCallbackUrl { get; set; }

    }
}
