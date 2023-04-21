using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerifyInAppPurchase.Models
{
    public class AndroidInAPPPurchaseVerifyResponseModel
    {
        public string StartTimeMillis { get; set; }
        public string ExpiryTimeMillis { get; set; }
        public bool AutoRenewing { get; set; }
        public string priceCurrencyCode { get; set; }
        public string PriceAmountMicros { get; set; }
        public string CountryCode { get; set; }
        public string DeveloperPayload { get; set; }
        public int PaymentState { get; set; }
        public string OrderId { get; set; }
        public int PurchaseType { get; set; }
        public int AcknowledgementState { get; set; }
        public string Kind { get; set; }
    }
}
