using System.Text.Json;
using System.Text.Json.Serialization;
using VerifyInAppPurchase.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VerifyInAppPurchase.Modules.System.Text.Json;
using Newtonsoft.Json;

namespace VerifyInAppPurchase
{
    public interface IInAppPurchaseReceiptVerifier
    {
        /// <summary>
        /// Send a receipt to the App Store for verification.
        /// </summary>
        /// <param name="receiptData">The Base64-encoded receipt data.</param>
        /// <param name="excludeOldTransactions">Set this value to <b>true</b> for the response to include only the latest renewal transaction for any subscriptions. Default: <b>false</b>.</param>
        Task<VerifyReceiptResponse> VerifyAppleReceiptAsync(string receiptData, bool excludeOldTransactions = false);
        /// <summary>
        /// Send a token to the google for verification 
        /// </summary>
        /// <param name="token">Id token which return from google after purchasing the plan</param>
        /// <param name="packageName">Package name </param>
        /// <param name="subscriptionId">plan id</param>
        /// <param name="googleaccesstoken">google access token which is generated using google service account.</param>
        /// <returns></returns>
        Task<AndroidInAPPPurchaseVerifyResponseModel>
            VerifyAndroidInAppPurchase(string token, string packageName, string subscriptionId, string googleaccesstoken);
    }

    public class InAppPurchaseReceiptVerifier : IInAppPurchaseReceiptVerifier
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy(),
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new QuotedConverter(),
                new CustomEnumConverterFactory(),
                new TimestampToDateTimeOffsetConverter()
            }
        };

        internal readonly AppleReceiptVerifierOptions Options;
        readonly HttpClient _httpClient;

        [ActivatorUtilitiesConstructor]
        public InAppPurchaseReceiptVerifier(IOptions<AppleReceiptVerifierOptions> options, HttpClient httpClient)
        {
            Options = options.Value;
            _httpClient = httpClient;
        }

        public InAppPurchaseReceiptVerifier(AppleReceiptVerifierOptions options, HttpClient httpClient)
        {
            Options = options;
            _httpClient = httpClient;
        }
        public async Task<VerifyReceiptResponse> VerifyAppleReceiptAsync(string receiptData, bool excludeOldTransactions = false)
        {
            var requestObj = new VerifyReceiptRequest(receiptData, Options.AppleAppSecret, excludeOldTransactions);
            string requestJson = System.Text.Json.JsonSerializer.Serialize(requestObj);
            var verifiedReceipt = await VerifyAppleReceiptInternalAsync(AppleReceiptVerifierOptions.ProductionEnvironmentUrl, requestJson).ConfigureAwait(false);
            if ((KnownStatusCodes)verifiedReceipt.Status == KnownStatusCodes.ReceiptIsFromTestEnvironment && Options.AcceptTestEnvironmentReceipts)
                verifiedReceipt = await VerifyAppleReceiptInternalAsync(AppleReceiptVerifierOptions.TestEnvironmentUrl, requestJson).ConfigureAwait(false);
            return verifiedReceipt;
        }

        async Task<VerifyReceiptResponse> VerifyAppleReceiptInternalAsync(string environmentUrl, string requestBodyJson)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, environmentUrl);
            req.Content = new JsonContent(requestBodyJson);
            string rawResp = await (await _httpClient.SendAsync(req).ConfigureAwait(false))
                .Content.ReadAsStringAsync()
                .ConfigureAwait(false);
            var verifiedReceipt = DeserializeResponse(rawResp);
            return verifiedReceipt;
        }

        internal static VerifyReceiptResponse DeserializeResponse(string rawJson)
        {
            var resp = System.Text.Json.JsonSerializer.Deserialize<VerifyReceiptResponse>(rawJson, JsonSerializerOptions)!;
            resp.RawJson = rawJson;
            return resp;
        }

        //Android
        public async Task<AndroidInAPPPurchaseVerifyResponseModel>
            VerifyAndroidInAppPurchase(string token, string packageName, string subscriptionId, string googleaccesstoken)
        {
            // var Authtokenresult = await GetGoogleAccessToken();
            var _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + googleaccesstoken);
            var result = await _client.GetAsync("https://androidpublisher.googleapis.com/androidpublisher/v3/applications/"
                + packageName + "/purchases/subscriptions/"
                + subscriptionId + "/tokens/"
                + token + "?access-token="
                + googleaccesstoken);
            if (result.IsSuccessStatusCode)
            {
                var result_ = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<AndroidInAPPPurchaseVerifyResponseModel>(result_);
            }
            else
            {
                var result_ = await result.Content.ReadAsStringAsync();
                return null;
            }

        }
    }
}