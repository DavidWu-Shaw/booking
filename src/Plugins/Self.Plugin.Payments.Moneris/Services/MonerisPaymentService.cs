using System;
using Moneris;
using Nop.Core.Domain.Payments;
using Nop.Services.Payments;

namespace Self.Plugin.Payments.Moneris.Services
{
    public class MonerisPaymentService : IPaymentGatewayService
    {
        private const string AVS_VISA_MATCH = "P";
        private const string AVS_MASTER_OR_DISCOVER_MATCH = "W";
        private const string AVS_AMEX_OR_JCB_MATCH = "Z";

        private const string CARDTYPE_VISA = "V";
        private const string CARDTYPE_MASTER = "M";
        private const string CARDTYPE_AMEX = "AX";

        private MonerisPaymentSettings _monerisPaymentSettings { get; set; }
        private HttpsPostRequest _transactionRequest { get; set; }

        public MonerisPaymentService(MonerisPaymentSettings monerisPaymentSettings)
        {
            _monerisPaymentSettings = monerisPaymentSettings;

            _transactionRequest = new HttpsPostRequest();
            _transactionRequest.SetProcCountryCode(_monerisPaymentSettings.ProcessingCountryCode);
            _transactionRequest.SetTestMode(_monerisPaymentSettings.UseSandbox);
            _transactionRequest.SetStoreId(_monerisPaymentSettings.StoreId);
            _transactionRequest.SetApiToken(_monerisPaymentSettings.ApiToken);
        }

        public ProcessPaymentResult Charge(ProcessPaymentRequest paymentRequest, bool validateAVS = false, bool validateCVD = false)
        {
            if (!validateAVS && !validateCVD)
            {
                return ChargeWithoutValidation(paymentRequest);
            }
            // Charge with CVD check or AVS check
            // Step 1: Authorize 
            var result = Authorize(paymentRequest);
            // Step 2: check AVS and CVD result
            bool isValid = false;
            if (validateAVS)
            {
                // Check AVS result
                string avsCode = result.AvsResult;
                isValid = avsCode == AVS_VISA_MATCH || avsCode == AVS_MASTER_OR_DISCOVER_MATCH || avsCode == AVS_AMEX_OR_JCB_MATCH;
            }
            if (validateCVD)
            {
                isValid = isValid && result.Cvv2Result == "1M";
                // Check CVD result
            }
            // Step 3: Perform Authorization Completion transaction based on check result
            var completionAmount = isValid ? paymentRequest.OrderTotal.ToString() : "0.00";
            var completion = CreateCompletion(paymentRequest.OrderGuid.ToString(), completionAmount, result.AuthorizationTransactionId);
            try
            {
                var receipt = PerformTransation(completion);
                // Build result
                ConvertToChargeResult(receipt, result);
            }
            catch (Exception ex)
            {
                result.AddError(ex.Message);
            }

            return result;
        }

        public ProcessPaymentResult ChargeWithoutValidation(ProcessPaymentRequest paymentRequest)
        {
            var result = new ProcessPaymentResult();
            var dataKey = paymentRequest.CustomValues["SavedCardVault"] as string;
            var isNewCardSaveAllowed = System.Convert.ToBoolean(paymentRequest.CustomValues["IsNewCardSaveAllowed"]);
            // Expire date "YYMM" format
            var expireDate = GetExpireDate(paymentRequest.CreditCardExpireYear, paymentRequest.CreditCardExpireMonth);

            try
            {
                if (isNewCardSaveAllowed)
                {
                    // Vault Add card
                    dataKey = AddCardToVault(paymentRequest.CreditCardNumber, expireDate, paymentRequest.CustomerId.ToString());
                    // TODO: save into database

                }
                Receipt receipt = null;
                if (!string.IsNullOrEmpty(dataKey))
                {
                    // Create purchase with dataKey
                    var purchase = CreatePurchaseWithVault(dataKey, paymentRequest.OrderGuid.ToString(),
                        paymentRequest.OrderTotal.ToString(), paymentRequest.CreditCardCvv2);

                    receipt = PerformTransation(purchase);
                }
                else
                {
                    // Create purchase
                    var purchase = CreatePurchase(paymentRequest.CreditCardNumber, expireDate, paymentRequest.CustomerId.ToString(),
                        paymentRequest.OrderGuid.ToString(), paymentRequest.OrderTotal.ToString(), paymentRequest.CreditCardCvv2);

                    receipt = PerformTransation(purchase);
                }
                // Build result
                ConvertToChargeResult(receipt, result);
            }
            catch (Exception ex)
            {
                result.NewPaymentStatus = PaymentStatus.Pending;
                result.AddError(ex.Message);
            }

            return result;
        }

        public ProcessPaymentResult Authorize(ProcessPaymentRequest paymentRequest)
        {
            var result = new ProcessPaymentResult();

            var dataKey = paymentRequest.CustomValues["SavedCardVault"] as string;
            var isNewCardSaveAllowed = System.Convert.ToBoolean(paymentRequest.CustomValues["IsNewCardSaveAllowed"]);
            // Expire date "YYMM" format
            var expireDate = GetExpireDate(paymentRequest.CreditCardExpireYear, paymentRequest.CreditCardExpireMonth);

            try
            {
                if (isNewCardSaveAllowed)
                {
                    // Vault Add card
                    dataKey = AddCardToVault(paymentRequest.CreditCardNumber, expireDate, paymentRequest.CustomerId.ToString());
                    // TODO: save into database

                }
                Receipt receipt = PerformAuthorizeTransation(dataKey, paymentRequest.CreditCardNumber, expireDate, paymentRequest.CustomerId.ToString(),
                    paymentRequest.OrderGuid.ToString(), paymentRequest.OrderTotal.ToString(), paymentRequest.CreditCardCvv2);

                result.AvsResult = receipt.GetAvsResultCode();
                result.Cvv2Result = receipt.GetCvdResultCode();
                result.AuthorizationTransactionId = receipt.GetTxnNumber();
                result.AuthorizationTransactionCode = receipt.GetAuthCode();
                result.AuthorizationTransactionResult = receipt.GetResponseCode();
            }
            catch (Exception ex)
            {
                result.AddError(ex.Message);
            }

            return result;
        }

        // TODO: need refactor
        public ProcessPaymentResult Capture(string orderId, string amount, string authTransactionNumber)
        {
            var result = new ProcessPaymentResult();

            var completion = CreateCompletion(orderId, amount, authTransactionNumber);

            try
            {
                var receipt = PerformTransation(completion);
                // Build result

            }
            catch (Exception ex)
            {
                result.AddError(ex.Message);
            }

            return result;
        }

        private string AddCardToVault(string creditCardNumber, string expireDate, string customerId)
        {
            var resaddcc = new ResAddCC();
            resaddcc.SetPan(creditCardNumber);
            resaddcc.SetExpDate(expireDate);
            resaddcc.SetCustId(customerId);
            resaddcc.SetCryptType(_monerisPaymentSettings.Crypt);
            resaddcc.SetGetCardType("true");

            var receipt = PerformTransation(resaddcc);
            var dataKey = receipt.GetDataKey();

            return dataKey;
        }

        private ResPurchaseCC CreatePurchaseWithVault(string dataKey, string orderId, string amount, string cvd)
        {
            // Create purchase with dataKey
            var purchase = new ResPurchaseCC();
            purchase.SetDataKey(dataKey);
            purchase.SetOrderId(orderId);
            purchase.SetAmount(amount);
            purchase.SetCryptType(_monerisPaymentSettings.Crypt);
            purchase.SetDynamicDescriptor(_monerisPaymentSettings.DynamicDescriptor);
            // CVD check
            purchase.SetCvdInfo(CreateCvdInfo(cvd));

            return purchase;
        }

        private Purchase CreatePurchase(string cardNumber, string expireDate, string customerId, string orderId, string amount, string cvd)
        {
            // Create purchase
            var purchase = new Purchase();
            purchase.SetPan(cardNumber);
            // Expire date "YYMM" format
            purchase.SetExpDate(expireDate);
            purchase.SetOrderId(orderId);
            purchase.SetCustId(customerId);
            purchase.SetAmount(amount);
            purchase.SetCryptType(_monerisPaymentSettings.Crypt);
            purchase.SetDynamicDescriptor(_monerisPaymentSettings.DynamicDescriptor);
            // CVD check
            purchase.SetCvdInfo(CreateCvdInfo(cvd));

            return purchase;
        }

        private ResPreauthCC CreateAuthWithVault(string dataKey, string orderId, string amount, string cvd)
        {
            // Create purchase with dataKey
            var auth = new ResPreauthCC();
            auth.SetDataKey(dataKey);
            auth.SetOrderId(orderId);
            auth.SetAmount(amount);
            auth.SetCryptType(_monerisPaymentSettings.Crypt);
            auth.SetDynamicDescriptor(_monerisPaymentSettings.DynamicDescriptor);
            // CVD check
            auth.SetCvdInfo(CreateCvdInfo(cvd));

            return auth;
        }

        private PreAuth CreateAuth(string cardNumber, string expireDate, string customerId, string orderId, string amount, string cvd)
        {
            // Create purchase
            var auth = new PreAuth();
            auth.SetPan(cardNumber);
            // Expire date "YYMM" format
            auth.SetExpDate(expireDate);
            auth.SetOrderId(orderId);
            auth.SetCustId(customerId);
            auth.SetAmount(amount);
            auth.SetCryptType(_monerisPaymentSettings.Crypt);
            auth.SetDynamicDescriptor(_monerisPaymentSettings.DynamicDescriptor);
            // CVD check
            auth.SetCvdInfo(CreateCvdInfo(cvd));

            return auth;
        }

        private Completion CreateCompletion(string orderId, string amount, string authTransactionNumber)
        {
            var completion = new Completion();
            completion.SetOrderId(orderId);
            completion.SetCompAmount(amount);
            completion.SetTxnNumber(authTransactionNumber);
            completion.SetCryptType(_monerisPaymentSettings.Crypt);
            completion.SetDynamicDescriptor(_monerisPaymentSettings.DynamicDescriptor);
            return completion;
        }

        private CvdInfo CreateCvdInfo(string cvd)
        {
            CvdInfo cvdCheck = new CvdInfo();
            cvdCheck.SetCvdIndicator("1");
            cvdCheck.SetCvdValue(cvd);
            return cvdCheck;
        }

        private Receipt PerformTransation(Transaction transaction)
        {
            _transactionRequest.SetTransaction(transaction);
            _transactionRequest.Send();
            return _transactionRequest.GetReceipt();
        }

        private Receipt PerformAuthorizeTransation(string dataKey, string cardNumber, string expireDate, string customerId, string orderId, string amount, string cvd)
        {
            Receipt receipt = null;
            if (!string.IsNullOrEmpty(dataKey))
            {
                // Create auth with dataKey
                var auth = CreateAuthWithVault(dataKey, orderId, amount, cvd);
                receipt = PerformTransation(auth);
            }
            else
            {
                // Create auth
                var auth = CreateAuth(cardNumber, expireDate, customerId, orderId, amount, cvd);
                receipt = PerformTransation(auth);
            }

            return receipt;
        }

        private void ConvertToChargeResult(Receipt receipt, ProcessPaymentResult result)
        {
            if (receipt.GetComplete() == "false")
            {
                result.AddError("Payment is not successful. Please try again or check payment option.");
            }
            result.AvsResult = receipt.GetAvsResultCode();
            result.Cvv2Result = receipt.GetCavvResultCode();
            result.AuthorizationTransactionId = receipt.GetReferenceNum();
            result.AuthorizationTransactionCode = receipt.GetAuthCode();
            result.AuthorizationTransactionResult = receipt.GetResponseCode();
            result.CaptureTransactionId = receipt.GetTxnNumber(); // required for refund
            result.CaptureTransactionResult = receipt.GetTransactionId();
            if (receipt.GetResponseCode() != null || receipt.GetResponseCode() != "null")
            {
                int responseCode = Int32.Parse(receipt.GetResponseCode().ToString());
                //Moneris reference
                //Transaction Response Code < 50: Transaction approved >= 50: Transaction declined 
                //NULL: Transaction was not sent for authorization For further details on the response codes that are returned please see the Response Codes table
                if (responseCode < 50)
                {
                    result.NewPaymentStatus = PaymentStatus.Paid;
                }
            }
        }

        private string GetExpireDate(int year, int month)
        {
            return string.Format("{0:D2}{1:D2}", year, month);
        }
    }
}
