using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Self.Plugin.Payments.Moneris.Models;

namespace Self.Plugin.Payments.Moneris.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class PaymentMonerisController : BasePaymentController
    {
        #region Fields
        
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;

        #endregion

        #region Ctor

        public PaymentMonerisController(ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext)
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
        }

        #endregion

        #region Methods

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var manualPaymentSettings = _settingService.LoadSetting<MonerisPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                StoreId = manualPaymentSettings.StoreId,
                ApiToken = manualPaymentSettings.ApiToken,
                ProcessingCountryCode = manualPaymentSettings.ProcessingCountryCode,
                UseSandbox = manualPaymentSettings.UseSandbox,
                DynamicDescriptor = manualPaymentSettings.DynamicDescriptor,
                Crypt = manualPaymentSettings.Crypt,
            };

            return View("~/Plugins/Payments.Moneris/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var manualPaymentSettings = _settingService.LoadSetting<MonerisPaymentSettings>(storeScope);

            //save settings
            manualPaymentSettings.StoreId = model.StoreId;
            manualPaymentSettings.ApiToken = model.ApiToken;
            manualPaymentSettings.ProcessingCountryCode = model.ProcessingCountryCode;
            manualPaymentSettings.UseSandbox = model.UseSandbox;
            manualPaymentSettings.DynamicDescriptor = model.DynamicDescriptor;
            manualPaymentSettings.Crypt = model.Crypt;

            //now clear settings cache
            _settingService.ClearCache();

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        #endregion
    }
}