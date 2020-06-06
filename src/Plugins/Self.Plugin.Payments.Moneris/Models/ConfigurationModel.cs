using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Self.Plugin.Payments.Moneris.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.Moneris.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        [NopResourceDisplayName("Plugins.Payments.Moneris.Fields.StoreId")]
        public string StoreId { get; set; }
        [NopResourceDisplayName("Plugins.Payments.Moneris.Fields.ApiToken")]
        public string ApiToken { get; set; }
        [NopResourceDisplayName("Plugins.Payments.Moneris.Fields.ProcessingCountryCode")]
        public string ProcessingCountryCode { get; set; }
        [NopResourceDisplayName("Plugins.Payments.Moneris.Fields.DynamicDescriptor")]
        public string DynamicDescriptor { get; set; }
        [NopResourceDisplayName("Plugins.Payments.Moneris.Fields.Crypt")]
        public string Crypt { get; set; }
    }
}