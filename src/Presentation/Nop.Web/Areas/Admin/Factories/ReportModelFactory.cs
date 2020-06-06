using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Web.Areas.Admin.Models.Reports;
using Nop.Web.Framework.Models.DataTables;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Web.Areas.Admin.Factories
{
    /// <summary>
    /// Represents the report model factory implementation
    /// </summary>
    public partial class ReportModelFactory : IReportModelFactory
    {
        #region Fields

        private readonly IBaseAdminModelFactory _baseAdminModelFactory;
        private readonly ICountryService _countryService;
        private readonly ICustomerReportService _customerReportService;
        private readonly ICustomerService _customerService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ILocalizationService _localizationService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IProductService _productService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public ReportModelFactory(IBaseAdminModelFactory baseAdminModelFactory,
            ICountryService countryService,
            ICustomerReportService customerReportService,
            ICustomerService customerService,
            IDateTimeHelper dateTimeHelper,
            ILocalizationService localizationService,
            IPriceFormatter priceFormatter,
            IProductAttributeFormatter productAttributeFormatter,
            IProductService productService,
            IWorkContext workContext)
        {
            _baseAdminModelFactory = baseAdminModelFactory;
            _countryService = countryService;
            _customerReportService = customerReportService;
            _customerService = customerService;
            _dateTimeHelper = dateTimeHelper;
            _localizationService = localizationService;
            _priceFormatter = priceFormatter;
            _productAttributeFormatter = productAttributeFormatter;
            _productService = productService;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        #region Customer reports

        /// <summary>
        /// Prepare customer reports search model
        /// </summary>
        /// <param name="searchModel">Customer reports search model</param>
        /// <returns>Customer reports search model</returns>
        public virtual CustomerReportsSearchModel PrepareCustomerReportsSearchModel(CustomerReportsSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare nested search models
            PrepareRegisteredCustomersReportSearchModel(searchModel.RegisteredCustomers);

            return searchModel;
        }

        /// <summary>
        /// Prepare registered customers report search model
        /// </summary>
        /// <param name="searchModel">Registered customers report search model</param>
        /// <returns>Registered customers report search model</returns>
        protected virtual RegisteredCustomersReportSearchModel PrepareRegisteredCustomersReportSearchModel(RegisteredCustomersReportSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged best customers report list model
        /// </summary>
        /// <param name="searchModel">Best customers report search model</param>
        /// <returns>Best customers report list model</returns>
        public virtual BestCustomersReportListModel PrepareBestCustomersReportListModel(BestCustomersReportSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get parameters to filter
            var startDateValue = !searchModel.StartDate.HasValue ? null
                : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.StartDate.Value, _dateTimeHelper.CurrentTimeZone);
            var endDateValue = !searchModel.EndDate.HasValue ? null
                : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.EndDate.Value, _dateTimeHelper.CurrentTimeZone).AddDays(1);
            var orderStatus = searchModel.OrderStatusId > 0 ? (OrderStatus?)searchModel.OrderStatusId : null;
            var paymentStatus = searchModel.PaymentStatusId > 0 ? (PaymentStatus?)searchModel.PaymentStatusId : null;
            var shippingStatus = searchModel.ShippingStatusId > 0 ? (ShippingStatus?)searchModel.ShippingStatusId : null;

            //get report items
            var reportItems = _customerReportService.GetBestCustomersReport(createdFromUtc: startDateValue,
                createdToUtc: endDateValue,
                os: orderStatus,
                ps: paymentStatus,
                ss: shippingStatus,
                orderBy: searchModel.OrderBy,
                pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

            //prepare list model
            var model = new BestCustomersReportListModel().PrepareToGrid(searchModel, reportItems, () =>
            {
                return reportItems.Select(item =>
               {
                    //fill in model values from the entity
                    var bestCustomersReportModel = new BestCustomersReportModel
                   {
                       CustomerId = item.CustomerId,
                       OrderTotal = _priceFormatter.FormatPrice(item.OrderTotal, true, false),
                       OrderCount = item.OrderCount
                   };

                    //fill in additional values (not existing in the entity)
                    var customer = _customerService.GetCustomerById(item.CustomerId);
                   if (customer != null)
                   {
                       bestCustomersReportModel.CustomerName = customer.IsRegistered() ? customer.Email :
                           _localizationService.GetResource("Admin.Customers.Guest");
                   }

                   return bestCustomersReportModel;
               });
            });

            return model;
        }
                
        /// <summary>
        /// Prepare paged registered customers report list model
        /// </summary>
        /// <param name="searchModel">Registered customers report search model</param>
        /// <returns>Registered customers report list model</returns>
        public virtual RegisteredCustomersReportListModel PrepareRegisteredCustomersReportListModel(RegisteredCustomersReportSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get report items
            var reportItems = new List<RegisteredCustomersReportModel>
            {
                new RegisteredCustomersReportModel
                {
                    Period = _localizationService.GetResource("Admin.Reports.Customers.RegisteredCustomers.Fields.Period.7days"),
                    Customers = _customerReportService.GetRegisteredCustomersReport(7)
                },
                new RegisteredCustomersReportModel
                {
                    Period = _localizationService.GetResource("Admin.Reports.Customers.RegisteredCustomers.Fields.Period.14days"),
                    Customers = _customerReportService.GetRegisteredCustomersReport(14)
                },
                new RegisteredCustomersReportModel
                {
                    Period = _localizationService.GetResource("Admin.Reports.Customers.RegisteredCustomers.Fields.Period.month"),
                    Customers = _customerReportService.GetRegisteredCustomersReport(30)
                },
                new RegisteredCustomersReportModel
                {
                    Period = _localizationService.GetResource("Admin.Reports.Customers.RegisteredCustomers.Fields.Period.year"),
                    Customers = _customerReportService.GetRegisteredCustomersReport(365)
                }
            };

            var pagedList = reportItems.ToPagedList(searchModel);

            //prepare list model
            var model = new RegisteredCustomersReportListModel().PrepareToGrid(searchModel, pagedList, () =>
            {
                return pagedList;
            });

            return model;
        }

        #endregion

        #endregion
    }
}