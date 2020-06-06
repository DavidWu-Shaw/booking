using Nop.Core.Configuration;

namespace Self.Plugin.Payments.Moneris
{
    /// <summary>
    /// Represents settings of manual payment plugin
    /// </summary>
    public class MonerisPaymentSettings : ISettings
    {
        public string ProcessingCountryCode { get; set; }
        /// <summary>
        /// Gets or sets a SecureNet identifier
        /// </summary>
        public string StoreId { get; set; }

        /// <summary>
        /// Gets or sets a secure key
        /// </summary>
        public string ApiToken { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to use sandbox (testing environment)
        /// </summary>
        public bool UseSandbox { get; set; }
        /// <summary>
        /// dynamic_descriptor
        /// </summary>
        public string DynamicDescriptor { get; set; }

        /// <summary>
        /// Crypt
        /// </summary>
        public string Crypt { get; set; }
    }
}
