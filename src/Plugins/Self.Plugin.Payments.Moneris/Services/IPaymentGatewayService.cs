using Nop.Services.Payments;

namespace Self.Plugin.Payments.Moneris.Services
{
    public interface IPaymentGatewayService
    {
        /// <summary>
        /// A Purchase verifies funds on the customer’s card, removes the funds and prepares them for deposit into the merchant’s account.
        /// Charge = Authorize + Capture
        /// Check AVS/CVD, if failed then void it otherwise complete it
        /// </summary>
        /// <param name="paymentRequest"></param>
        /// <param name="validateAVS"></param>
        /// <param name="validateCVD"></param>
        /// <returns></returns>
        ProcessPaymentResult Charge(ProcessPaymentRequest paymentRequest, bool validateAVS = false, bool validateCVD = false);
        /// <summary>
        /// Verifies and locks funds on the customer’s credit card. The funds are locked for a specified amount of time 
        /// based on the card issuer. To retrieve the funds that have been locked by a Authorization transaction 
        /// so that they may be settled in the merchant’s account, a Capture transaction must be performed.
        /// </summary>
        /// <param name="paymentRequest"></param>
        /// <returns>Payment result</returns>
        ProcessPaymentResult Authorize(ProcessPaymentRequest paymentRequest);
    }
}
