using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Self;
using Nop.Core.Domain.Vendors;
using Nop.Core.Infrastructure;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.ExportImport;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Self;
using Nop.Services.Seo;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Helpers;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Areas.Admin.Models.Self;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nop.Web.Areas.Admin.Controllers
{
    public partial class ProductController : BaseAdminController
    {
        public const int MORNING_SHIFT_STARTS = 9;
        public const int MORNING_SHIFT_ENDS = 13;
        public const int AFTERNOON_SHIFT_STARTS = 14;
        public const int AFTERNOON_SHIFT_ENDS = 18;

        #region Fields

        private readonly IAclService _aclService;
        private readonly ICategoryService _categoryService;
        private readonly ICopyProductService _copyProductService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ICustomerService _customerService;
        private readonly IExportManager _exportManager;
        private readonly IImportManager _importManager;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly INopFileProvider _fileProvider;
        private readonly INotificationService _notificationService;
        private readonly IPdfService _pdfService;
        private readonly IPermissionService _permissionService;
        private readonly IPictureService _pictureService;
        private readonly IProductModelFactory _productModelFactory;
        private readonly IAppointmentModelFactory _appointmentModelFactory;
        private readonly IProductService _productService;
        private readonly IAppointmentService _appointmentService;
        private readonly IProductTagService _productTagService;
        private readonly ISettingService _settingService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWorkContext _workContext;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly VendorSettings _vendorSettings;

        #endregion

        #region Ctor

        public ProductController(IAclService aclService,
            ICategoryService categoryService,
            ICopyProductService copyProductService,
            ICustomerActivityService customerActivityService,
            ICustomerService customerService,
            IExportManager exportManager,
            IImportManager importManager,
            ILanguageService languageService,
            ILocalizationService localizationService,
            ILocalizedEntityService localizedEntityService,
            INopFileProvider fileProvider,
            INotificationService notificationService,
            IPdfService pdfService,
            IPermissionService permissionService,
            IPictureService pictureService,
            IProductModelFactory productModelFactory,
            IAppointmentModelFactory appointmentModelFactory,
            IProductService productService,
            IAppointmentService appointmentService,
            IProductTagService productTagService,
            ISettingService settingService,
            IUrlRecordService urlRecordService,
            IWorkContext workContext,
            IDateTimeHelper dateTimeHelper,
            VendorSettings vendorSettings)
        {
            _aclService = aclService;
            _categoryService = categoryService;
            _copyProductService = copyProductService;
            _customerActivityService = customerActivityService;
            _customerService = customerService;
            _exportManager = exportManager;
            _importManager = importManager;
            _languageService = languageService;
            _localizationService = localizationService;
            _localizedEntityService = localizedEntityService;
            _fileProvider = fileProvider;
            _notificationService = notificationService;
            _pdfService = pdfService;
            _permissionService = permissionService;
            _pictureService = pictureService;
            _productModelFactory = productModelFactory;
            _appointmentModelFactory = appointmentModelFactory;
            _productService = productService;
            _appointmentService = appointmentService;
            _productTagService = productTagService;
            _settingService = settingService;
            _urlRecordService = urlRecordService;
            _workContext = workContext;
            _dateTimeHelper = dateTimeHelper;
            _vendorSettings = vendorSettings;
        }

        #endregion

        #region Utilities

        protected virtual void UpdateLocales(Product product, ProductModel model)
        {
            foreach (var localized in model.Locales)
            {
                _localizedEntityService.SaveLocalizedValue(product,
                    x => x.Name,
                    localized.Name,
                    localized.LanguageId);
                _localizedEntityService.SaveLocalizedValue(product,
                    x => x.ShortDescription,
                    localized.ShortDescription,
                    localized.LanguageId);
                _localizedEntityService.SaveLocalizedValue(product,
                    x => x.FullDescription,
                    localized.FullDescription,
                    localized.LanguageId);
                _localizedEntityService.SaveLocalizedValue(product,
                    x => x.MetaKeywords,
                    localized.MetaKeywords,
                    localized.LanguageId);
                _localizedEntityService.SaveLocalizedValue(product,
                    x => x.MetaDescription,
                    localized.MetaDescription,
                    localized.LanguageId);
                _localizedEntityService.SaveLocalizedValue(product,
                    x => x.MetaTitle,
                    localized.MetaTitle,
                    localized.LanguageId);

                //search engine name
                var seName = _urlRecordService.ValidateSeName(product, localized.SeName, localized.Name, false);
                _urlRecordService.SaveSlug(product, seName, localized.LanguageId);
            }
        }

        protected virtual void UpdateLocales(ProductTag productTag, ProductTagModel model)
        {
            foreach (var localized in model.Locales)
            {
                _localizedEntityService.SaveLocalizedValue(productTag,
                    x => x.Name,
                    localized.Name,
                    localized.LanguageId);

                var seName = _urlRecordService.ValidateSeName(productTag, string.Empty, localized.Name, false);
                _urlRecordService.SaveSlug(productTag, seName, localized.LanguageId);
            }
        }

        protected virtual void UpdatePictureSeoNames(Product product)
        {
            foreach (var pp in product.ProductPictures)
                _pictureService.SetSeoFilename(pp.PictureId, _pictureService.GetPictureSeName(product.Name));
        }

        protected virtual void SaveProductAcl(Product product, ProductModel model)
        {
            product.SubjectToAcl = model.SelectedCustomerRoleIds.Any();

            var existingAclRecords = _aclService.GetAclRecords(product);
            var allCustomerRoles = _customerService.GetAllCustomerRoles(true);
            foreach (var customerRole in allCustomerRoles)
            {
                if (model.SelectedCustomerRoleIds.Contains(customerRole.Id))
                {
                    //new role
                    if (existingAclRecords.Count(acl => acl.CustomerRoleId == customerRole.Id) == 0)
                        _aclService.InsertAclRecord(product, customerRole.Id);
                }
                else
                {
                    //remove role
                    var aclRecordToDelete = existingAclRecords.FirstOrDefault(acl => acl.CustomerRoleId == customerRole.Id);
                    if (aclRecordToDelete != null)
                        _aclService.DeleteAclRecord(aclRecordToDelete);
                }
            }
        }

        protected virtual void SaveCategoryMappings(Product product, ProductModel model)
        {
            var existingProductCategories = _categoryService.GetProductCategoriesByProductId(product.Id, true);

            //delete categories
            foreach (var existingProductCategory in existingProductCategories)
                if (!model.SelectedCategoryIds.Contains(existingProductCategory.CategoryId))
                    _categoryService.DeleteProductCategory(existingProductCategory);

            //add categories
            foreach (var categoryId in model.SelectedCategoryIds)
            {
                if (_categoryService.FindProductCategory(existingProductCategories, product.Id, categoryId) == null)
                {
                    //find next display order
                    var displayOrder = 1;
                    var existingCategoryMapping = _categoryService.GetProductCategoriesByCategoryId(categoryId, showHidden: true);
                    if (existingCategoryMapping.Any())
                        displayOrder = existingCategoryMapping.Max(x => x.DisplayOrder) + 1;
                    _categoryService.InsertProductCategory(new ProductCategory
                    {
                        ProductId = product.Id,
                        CategoryId = categoryId,
                        DisplayOrder = displayOrder
                    });
                }
            }
        }

        protected virtual string[] ParseProductTags(string productTags)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(productTags))
                return result.ToArray();

            var values = productTags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var val in values)
                if (!string.IsNullOrEmpty(val.Trim()))
                    result.Add(val.Trim());

            return result.ToArray();
        }

        #endregion

        #region Appointment Methods

        /// <summary>
        /// Get method for /Admin/Product/AppointmentSchedule/{productId}
        /// </summary>
        /// <param name="id">Product Id</param>
        /// <returns></returns>
        public virtual IActionResult AppointmentSchedule(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //try to get a product with the specified id
            var product = _productService.GetProductById(id);
            if (product == null || product.Deleted)
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && product.VendorId != _workContext.CurrentVendor.Id)
                return RedirectToAction("List");

            //prepare model
            var model = _productModelFactory.PrepareProductModel(null, product);

            return View(model);
        }

        /// <summary>
        /// Get method for /Admin/Product/AppointmentCalendar/{productId}
        /// </summary>
        /// <param name="id">Product Id</param>
        /// <returns></returns>
        public virtual IActionResult AppointmentCalendar(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //try to get a product with the specified id
            var product = _productService.GetProductById(id);
            if (product == null || product.Deleted)
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && product.VendorId != _workContext.CurrentVendor.Id)
                return RedirectToAction("List");

            //prepare model
            var model = _productModelFactory.PrepareProductModel(null, product);

            return View(model);
        }

        [HttpPost]
        public virtual IActionResult AppointmentList(DateTime start, DateTime end, int resourceId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProductReviews))
                return AccessDeniedDataTablesJson();

            var appointments = _appointmentService.GetAppointmentsByResource(start, end, resourceId);

            var model = new List<AppointmentInfoModel>();
            foreach (var appointment in appointments)
            {
                var item = _appointmentModelFactory.PrepareAppointmentInfoModel(appointment);
                model.Add(item);
            }

            return Json(model);
        }

        [HttpPost]
        public virtual IActionResult AppointmentCreate(DateTime start, DateTime end, int resourceId, string scale)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProductReviews))
                return AccessDeniedView();

            var timeSlots = GetSlots(start, end, scale);
            foreach (var slot in timeSlots)
            {
                Appointment appointment = new Appointment
                {
                    StartTimeUtc = slot.Start.ToUniversalTime(),
                    EndTimeUtc = slot.End.ToUniversalTime(),
                    ResourceId = resourceId,
                    Notes = string.Empty,
                    Status = AppointmentStatusType.Free
                };

                _appointmentService.InsertAppointment(appointment);
            }

            return Json(new { status = true, responseText = $"{timeSlots.Count} records created." });
        }

        private List<TimeSlot> GetSlots(DateTime start, DateTime end, string scale)
        {
            if (scale == "shifts")
            {
                return GetSlotsByShift(start, end);
            }
            else
            {
                var helper = new AppointmentTimeSlotHelper(MORNING_SHIFT_STARTS, MORNING_SHIFT_ENDS, AFTERNOON_SHIFT_STARTS, AFTERNOON_SHIFT_ENDS);
                return helper.GetSlotsByHour(start, end);
            }
        }

        private List<TimeSlot> GetSlotsByShift(DateTime start, DateTime end)
        {
            var result = new List<TimeSlot>();

            return result;
        }

        /// <summary>
        /// Get method for /Admin/Product/AppointmentEdit/{appointmentId}
        /// </summary>
        /// <param name="id">Appointment Id</param>
        /// <returns></returns>
        public virtual IActionResult AppointmentEdit(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            var appointment = _appointmentService.GetAppointmentById(id);
            if (appointment != null)
            {
                //prepare model
                var model = _appointmentModelFactory.PrepareAppointmentEditModel(appointment);
                // TODO: remove admin user later
                model.IsLoggedInAsVendor = _workContext.CurrentVendor != null || _workContext.IsAdmin;
                return Json(new { status = true, data = model });
            }
            else
            {
                string statusText = _localizationService.GetResource("Admin.Product.AppointmentEdit.SlotNoExist");
                return Json(new { status = false, message = statusText });
            }
        }

        [HttpPost]
        public virtual IActionResult AppointmentConfirm(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProductReviews))
                return AccessDeniedView();

            var appointment = _appointmentService.GetAppointmentById(id);
            if (appointment != null && appointment.Status == AppointmentStatusType.Waiting)
            {
                appointment.Status = AppointmentStatusType.Confirmed;
                _appointmentService.UpdateAppointment(appointment);

                var model = _appointmentModelFactory.PrepareAppointmentEditModel(appointment);

                string statusText = _localizationService.GetResource("Admin.Product.AppointmentConfirm.Confirmed");
                return Json(new { status = true, message = statusText, data = model });
            }
            else
            {
                // Customer may have just concelled this appointment
                string statusText = _localizationService.GetResource("Admin.Product.AppointmentConfirm.Failed");
                return Json(new { status = false, message = statusText });
            }
        }

        [HttpPost]
        public virtual IActionResult AppointmentCancel(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProductReviews))
                return AccessDeniedView();

            var appointment = _appointmentService.GetAppointmentById(id);
            if (appointment != null && (appointment.Status == AppointmentStatusType.Waiting || appointment.Status == AppointmentStatusType.Confirmed))
            {
                appointment.Status = AppointmentStatusType.Free;
                appointment.CustomerId = 0;
                appointment.Notes = "";
                _appointmentService.UpdateAppointment(appointment);

                var model = _appointmentModelFactory.PrepareAppointmentEditModel(appointment);

                string statusText = _localizationService.GetResource("Admin.Product.AppointmentCancel.Cancelled");
                return Json(new { status = true, message = statusText, data = model });
            }
            else
            {
                string statusText = _localizationService.GetResource("Admin.Product.AppointmentCancel.Failed");
                return Json(new { status = false, message = statusText });
            }
        }

        [HttpPost]
        public virtual IActionResult AppointmentDelete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProductReviews))
                return AccessDeniedView();

            var appointment = _appointmentService.GetAppointmentById(id);
            if (appointment != null && appointment.Status == AppointmentStatusType.Free)
            {
                _appointmentService.DeleteAppointment(appointment);
                string statusText = _localizationService.GetResource("Admin.Product.AppointmentDelete.Deleted");
                return Json(new { status = true, message = statusText });
            }
            else
            {
                string statusText = _localizationService.GetResource("Admin.Product.AppointmentDelete.Failed");
                return Json(new { status = false, message = statusText });
            }
        }

        #endregion Appointment Methods

        #region Methods

        #region Product list / create / edit / delete

        public virtual IActionResult Index()
        {
            return RedirectToAction("List");
        }

        public virtual IActionResult List()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //prepare model
            var model = _productModelFactory.PrepareProductSearchModel(new ProductSearchModel());

            return View(model);
        }

        [HttpPost]
        public virtual IActionResult ProductList(ProductSearchModel searchModel)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedDataTablesJson();

            //prepare model
            var model = _productModelFactory.PrepareProductListModel(searchModel);

            return Json(model);
        }

        public virtual IActionResult Create()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //validate maximum number of products per vendor
            if (_vendorSettings.MaximumProductNumber > 0 && _workContext.CurrentVendor != null
                && _productService.GetNumberOfProductsByVendorId(_workContext.CurrentVendor.Id) >= _vendorSettings.MaximumProductNumber)
            {
                _notificationService.ErrorNotification(string.Format(_localizationService.GetResource("Admin.Catalog.Products.ExceededMaximumNumber"),
                    _vendorSettings.MaximumProductNumber));
                return RedirectToAction("List");
            }

            //prepare model
            var model = _productModelFactory.PrepareProductModel(new ProductModel(), null);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public virtual IActionResult Create(ProductModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //validate maximum number of products per vendor
            if (_vendorSettings.MaximumProductNumber > 0 && _workContext.CurrentVendor != null
                && _productService.GetNumberOfProductsByVendorId(_workContext.CurrentVendor.Id) >= _vendorSettings.MaximumProductNumber)
            {
                _notificationService.ErrorNotification(string.Format(_localizationService.GetResource("Admin.Catalog.Products.ExceededMaximumNumber"),
                    _vendorSettings.MaximumProductNumber));
                return RedirectToAction("List");
            }

            if (ModelState.IsValid)
            {
                //a vendor should have access only to his products
                if (_workContext.CurrentVendor != null)
                    model.VendorId = _workContext.CurrentVendor.Id;

                //vendors cannot edit "Show on home page" property
                if (_workContext.CurrentVendor != null && model.ShowOnHomepage)
                    model.ShowOnHomepage = false;

                //product
                var product = model.ToEntity<Product>();
                product.CreatedOnUtc = DateTime.UtcNow;
                product.UpdatedOnUtc = DateTime.UtcNow;
                _productService.InsertProduct(product);

                //search engine name
                model.SeName = _urlRecordService.ValidateSeName(product, model.SeName, product.Name, true);
                _urlRecordService.SaveSlug(product, model.SeName, 0);

                //locales
                UpdateLocales(product, model);

                //categories
                SaveCategoryMappings(product, model);

                //ACL (customer roles)
                SaveProductAcl(product, model);

                //tags
                _productTagService.UpdateProductTags(product, ParseProductTags(model.ProductTags));

                //activity log
                _customerActivityService.InsertActivity("AddNewProduct",
                    string.Format(_localizationService.GetResource("ActivityLog.AddNewProduct"), product.Name), product);

                _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Catalog.Products.Added"));

                if (!continueEditing)
                    return RedirectToAction("List");
                
                return RedirectToAction("Edit", new { id = product.Id });
            }

            //prepare model
            model = _productModelFactory.PrepareProductModel(model, null, true);

            //if we got this far, something failed, redisplay form
            return View(model);
        }

        public virtual IActionResult Edit(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //try to get a product with the specified id
            var product = _productService.GetProductById(id);
            if (product == null || product.Deleted)
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && product.VendorId != _workContext.CurrentVendor.Id)
                return RedirectToAction("List");

            //prepare model
            var model = _productModelFactory.PrepareProductModel(null, product);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public virtual IActionResult Edit(ProductModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //try to get a product with the specified id
            var product = _productService.GetProductById(model.Id);
            if (product == null || product.Deleted)
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && product.VendorId != _workContext.CurrentVendor.Id)
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                //a vendor should have access only to his products
                if (_workContext.CurrentVendor != null)
                    model.VendorId = _workContext.CurrentVendor.Id;

                //we do not validate maximum number of products per vendor when editing existing products (only during creation of new products)
                //vendors cannot edit "Show on home page" property
                if (_workContext.CurrentVendor != null && model.ShowOnHomepage != product.ShowOnHomepage)
                    model.ShowOnHomepage = product.ShowOnHomepage;

                //some previously used values
                var prevTotalStockQuantity = _productService.GetTotalStockQuantity(product);
                var prevDownloadId = product.DownloadId;
                var prevSampleDownloadId = product.SampleDownloadId;
                var previousStockQuantity = product.StockQuantity;
                var previousWarehouseId = product.WarehouseId;

                //product
                product = model.ToEntity(product);

                product.UpdatedOnUtc = DateTime.UtcNow;
                _productService.UpdateProduct(product);

                //search engine name
                model.SeName = _urlRecordService.ValidateSeName(product, model.SeName, product.Name, true);
                _urlRecordService.SaveSlug(product, model.SeName, 0);

                //locales
                UpdateLocales(product, model);

                //tags
                _productTagService.UpdateProductTags(product, ParseProductTags(model.ProductTags));

                //categories
                SaveCategoryMappings(product, model);

                //ACL (customer roles)
                SaveProductAcl(product, model);

                //stores
                _productService.UpdateProductStoreMappings(product, model.SelectedStoreIds);

                //picture seo names
                UpdatePictureSeoNames(product);

                //activity log
                _customerActivityService.InsertActivity("EditProduct",
                    string.Format(_localizationService.GetResource("ActivityLog.EditProduct"), product.Name), product);

                _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Catalog.Products.Updated"));

                if (!continueEditing)
                    return RedirectToAction("List");
                
                return RedirectToAction("Edit", new { id = product.Id });
            }

            //prepare model
            model = _productModelFactory.PrepareProductModel(model, product, true);

            //if we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        public virtual IActionResult Delete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //try to get a product with the specified id
            var product = _productService.GetProductById(id);
            if (product == null)
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && product.VendorId != _workContext.CurrentVendor.Id)
                return RedirectToAction("List");

            _productService.DeleteProduct(product);

            //activity log
            _customerActivityService.InsertActivity("DeleteProduct",
                string.Format(_localizationService.GetResource("ActivityLog.DeleteProduct"), product.Name), product);

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Catalog.Products.Deleted"));

            return RedirectToAction("List");
        }

        [HttpPost]
        public virtual IActionResult DeleteSelected(ICollection<int> selectedIds)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            if (selectedIds != null)
            {
                _productService.DeleteProducts(_productService.GetProductsByIds(selectedIds.ToArray()).Where(p => _workContext.CurrentVendor == null || p.VendorId == _workContext.CurrentVendor.Id).ToList());
            }

            return Json(new { Result = true });
        }

        [HttpPost]
        public virtual IActionResult CopyProduct(ProductModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            var copyModel = model.CopyProductModel;
            try
            {
                var originalProduct = _productService.GetProductById(copyModel.Id);

                //a vendor should have access only to his products
                if (_workContext.CurrentVendor != null && originalProduct.VendorId != _workContext.CurrentVendor.Id)
                    return RedirectToAction("List");

                var newProduct = _copyProductService.CopyProduct(originalProduct, copyModel.Name, copyModel.Published, copyModel.CopyImages);

                _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Catalog.Products.Copied"));

                return RedirectToAction("Edit", new { id = newProduct.Id });
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc.Message);
                return RedirectToAction("Edit", new { id = copyModel.Id });
            }
        }

        #endregion

        #region Required products

        [HttpPost]
        public virtual IActionResult LoadProductFriendlyNames(string productIds)
        {
            var result = string.Empty;

            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return Json(new { Text = result });

            if (string.IsNullOrWhiteSpace(productIds))
                return Json(new { Text = result });

            var ids = new List<int>();
            var rangeArray = productIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            foreach (var str1 in rangeArray)
            {
                if (int.TryParse(str1, out var tmp1))
                    ids.Add(tmp1);
            }

            var products = _productService.GetProductsByIds(ids.ToArray());
            for (var i = 0; i <= products.Count - 1; i++)
            {
                result += products[i].Name;
                if (i != products.Count - 1)
                    result += ", ";
            }

            return Json(new { Text = result });
        }

        #endregion

        #region Related products

        [HttpPost]
        public virtual IActionResult RelatedProductList(RelatedProductSearchModel searchModel)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedDataTablesJson();

            //try to get a product with the specified id
            var product = _productService.GetProductById(searchModel.ProductId)
                ?? throw new ArgumentException("No product found with the specified id");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && product.VendorId != _workContext.CurrentVendor.Id)
                return Content("This is not your product");

            //prepare model
            var model = _productModelFactory.PrepareRelatedProductListModel(searchModel, product);

            return Json(model);
        }

        [HttpPost]
        public virtual IActionResult RelatedProductUpdate(RelatedProductModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //try to get a related product with the specified id
            var relatedProduct = _productService.GetRelatedProductById(model.Id)
                ?? throw new ArgumentException("No related product found with the specified id");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null)
            {
                var product = _productService.GetProductById(relatedProduct.ProductId1);
                if (product != null && product.VendorId != _workContext.CurrentVendor.Id)
                    return Content("This is not your product");
            }

            relatedProduct.DisplayOrder = model.DisplayOrder;
            _productService.UpdateRelatedProduct(relatedProduct);

            return new NullJsonResult();
        }

        [HttpPost]
        public virtual IActionResult RelatedProductDelete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //try to get a related product with the specified id
            var relatedProduct = _productService.GetRelatedProductById(id)
                ?? throw new ArgumentException("No related product found with the specified id");

            var productId = relatedProduct.ProductId1;

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null)
            {
                var product = _productService.GetProductById(productId);
                if (product != null && product.VendorId != _workContext.CurrentVendor.Id)
                    return Content("This is not your product");
            }

            _productService.DeleteRelatedProduct(relatedProduct);

            return new NullJsonResult();
        }

        public virtual IActionResult RelatedProductAddPopup(int productId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //prepare model
            var model = _productModelFactory.PrepareAddRelatedProductSearchModel(new AddRelatedProductSearchModel());

            return View(model);
        }

        [HttpPost]
        public virtual IActionResult RelatedProductAddPopupList(AddRelatedProductSearchModel searchModel)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedDataTablesJson();

            //prepare model
            var model = _productModelFactory.PrepareAddRelatedProductListModel(searchModel);

            return Json(model);
        }

        [HttpPost]
        [FormValueRequired("save")]
        public virtual IActionResult RelatedProductAddPopup(AddRelatedProductModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            var selectedProducts = _productService.GetProductsByIds(model.SelectedProductIds.ToArray());
            if (selectedProducts.Any())
            {
                var existingRelatedProducts = _productService.GetRelatedProductsByProductId1(model.ProductId);
                foreach (var product in selectedProducts)
                {
                    //a vendor should have access only to his products
                    if (_workContext.CurrentVendor != null && product.VendorId != _workContext.CurrentVendor.Id)
                        continue;

                    if (_productService.FindRelatedProduct(existingRelatedProducts, model.ProductId, product.Id) != null)
                        continue;

                    _productService.InsertRelatedProduct(new RelatedProduct
                    {
                        ProductId1 = model.ProductId,
                        ProductId2 = product.Id,
                        DisplayOrder = 1
                    });
                }
            }

            ViewBag.RefreshPage = true;

            return View(new AddRelatedProductSearchModel());
        }

        #endregion

        #region Product pictures

        public virtual IActionResult ProductPictureAdd(int pictureId, int displayOrder,
            string overrideAltAttribute, string overrideTitleAttribute, int productId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            if (pictureId == 0)
                throw new ArgumentException();

            //try to get a product with the specified id
            var product = _productService.GetProductById(productId)
                ?? throw new ArgumentException("No product found with the specified id");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && product.VendorId != _workContext.CurrentVendor.Id)
                return RedirectToAction("List");

            if (_productService.GetProductPicturesByProductId(productId).Any(p => p.PictureId == pictureId))
                return Json(new { Result = false });

            //try to get a picture with the specified id
            var picture = _pictureService.GetPictureById(pictureId)
                ?? throw new ArgumentException("No picture found with the specified id");

            _pictureService.UpdatePicture(picture.Id,
                _pictureService.LoadPictureBinary(picture),
                picture.MimeType,
                picture.SeoFilename,
                overrideAltAttribute,
                overrideTitleAttribute);

            _pictureService.SetSeoFilename(pictureId, _pictureService.GetPictureSeName(product.Name));

            _productService.InsertProductPicture(new ProductPicture
            {
                PictureId = pictureId,
                ProductId = productId,
                DisplayOrder = displayOrder
            });

            return Json(new { Result = true });
        }

        [HttpPost]
        public virtual IActionResult ProductPictureList(ProductPictureSearchModel searchModel)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedDataTablesJson();

            //try to get a product with the specified id
            var product = _productService.GetProductById(searchModel.ProductId)
                ?? throw new ArgumentException("No product found with the specified id");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && product.VendorId != _workContext.CurrentVendor.Id)
                return Content("This is not your product");

            //prepare model
            var model = _productModelFactory.PrepareProductPictureListModel(searchModel, product);

            return Json(model);
        }

        [HttpPost]
        public virtual IActionResult ProductPictureUpdate(ProductPictureModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //try to get a product picture with the specified id
            var productPicture = _productService.GetProductPictureById(model.Id)
                ?? throw new ArgumentException("No product picture found with the specified id");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null)
            {
                var product = _productService.GetProductById(productPicture.ProductId);
                if (product != null && product.VendorId != _workContext.CurrentVendor.Id)
                    return Content("This is not your product");
            }

            //try to get a picture with the specified id
            var picture = _pictureService.GetPictureById(productPicture.PictureId)
                ?? throw new ArgumentException("No picture found with the specified id");

            _pictureService.UpdatePicture(picture.Id,
                _pictureService.LoadPictureBinary(picture),
                picture.MimeType,
                picture.SeoFilename,
                model.OverrideAltAttribute,
                model.OverrideTitleAttribute);

            productPicture.DisplayOrder = model.DisplayOrder;
            _productService.UpdateProductPicture(productPicture);

            return new NullJsonResult();
        }

        [HttpPost]
        public virtual IActionResult ProductPictureDelete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //try to get a product picture with the specified id
            var productPicture = _productService.GetProductPictureById(id)
                ?? throw new ArgumentException("No product picture found with the specified id");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null)
            {
                var product = _productService.GetProductById(productPicture.ProductId);
                if (product != null && product.VendorId != _workContext.CurrentVendor.Id)
                    return Content("This is not your product");
            }

            var pictureId = productPicture.PictureId;
            _productService.DeleteProductPicture(productPicture);

            //try to get a picture with the specified id
            var picture = _pictureService.GetPictureById(pictureId)
                ?? throw new ArgumentException("No picture found with the specified id");

            _pictureService.DeletePicture(picture);

            return new NullJsonResult();
        }

        #endregion

        #region Product tags

        public virtual IActionResult ProductTags()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProductTags))
                return AccessDeniedView();

            //prepare model
            var model = _productModelFactory.PrepareProductTagSearchModel(new ProductTagSearchModel());

            return View(model);
        }

        [HttpPost]
        public virtual IActionResult ProductTags(ProductTagSearchModel searchModel)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProductTags))
                return AccessDeniedDataTablesJson();

            //prepare model
            var model = _productModelFactory.PrepareProductTagListModel(searchModel);

            return Json(model);
        }

        [HttpPost]
        public virtual IActionResult ProductTagDelete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProductTags))
                return AccessDeniedView();

            //try to get a product tag with the specified id
            var tag = _productTagService.GetProductTagById(id)
                ?? throw new ArgumentException("No product tag found with the specified id");

            _productTagService.DeleteProductTag(tag);

            return new NullJsonResult();
        }

        public virtual IActionResult EditProductTag(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProductTags))
                return AccessDeniedView();

            //try to get a product tag with the specified id
            var productTag = _productTagService.GetProductTagById(id);
            if (productTag == null)
                return RedirectToAction("List");

            //prepare tag model
            var model = _productModelFactory.PrepareProductTagModel(null, productTag);

            return View(model);
        }

        [HttpPost]
        public virtual IActionResult EditProductTag(ProductTagModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProductTags))
                return AccessDeniedView();

            //try to get a product tag with the specified id
            var productTag = _productTagService.GetProductTagById(model.Id);
            if (productTag == null)
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                productTag.Name = model.Name;
                _productTagService.UpdateProductTag(productTag);

                //locales
                UpdateLocales(productTag, model);

                ViewBag.RefreshPage = true;
                return View(model);
            }

            //prepare model
            model = _productModelFactory.PrepareProductTagModel(model, productTag, true);

            //if we got this far, something failed, redisplay form
            return View(model);
        }

        #endregion

        #region Export / Import

        [HttpPost, ActionName("List")]
        [FormValueRequired("download-catalog-pdf")]
        public virtual IActionResult DownloadCatalogAsPdf(ProductSearchModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null)
            {
                model.SearchVendorId = _workContext.CurrentVendor.Id;
            }

            var categoryIds = new List<int> { model.SearchCategoryId };
            //include subcategories
            if (model.SearchIncludeSubCategories && model.SearchCategoryId > 0)
                categoryIds.AddRange(_categoryService.GetChildCategoryIds(parentCategoryId: model.SearchCategoryId, showHidden: true));

            //0 - all (according to "ShowHidden" parameter)
            //1 - published only
            //2 - unpublished only
            bool? overridePublished = null;
            if (model.SearchPublishedId == 1)
                overridePublished = true;
            else if (model.SearchPublishedId == 2)
                overridePublished = false;

            var products = _productService.SearchProducts(
                categoryIds: categoryIds,
                vendorId: model.SearchVendorId,
                productType: model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null,
                keywords: model.SearchProductName,
                showHidden: true,
                overridePublished: overridePublished);

            try
            {
                byte[] bytes;
                using (var stream = new MemoryStream())
                {
                    _pdfService.PrintProductsToPdf(stream, products);
                    bytes = stream.ToArray();
                }

                return File(bytes, MimeTypes.ApplicationPdf, "pdfcatalog.pdf");
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc);
                return RedirectToAction("List");
            }
        }

        [HttpPost, ActionName("List")]
        [FormValueRequired("exportxml-all")]
        public virtual IActionResult ExportXmlAll(ProductSearchModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null)
            {
                model.SearchVendorId = _workContext.CurrentVendor.Id;
            }

            var categoryIds = new List<int> { model.SearchCategoryId };
            //include subcategories
            if (model.SearchIncludeSubCategories && model.SearchCategoryId > 0)
                categoryIds.AddRange(_categoryService.GetChildCategoryIds(parentCategoryId: model.SearchCategoryId, showHidden: true));

            //0 - all (according to "ShowHidden" parameter)
            //1 - published only
            //2 - unpublished only
            bool? overridePublished = null;
            if (model.SearchPublishedId == 1)
                overridePublished = true;
            else if (model.SearchPublishedId == 2)
                overridePublished = false;

            var products = _productService.SearchProducts(
                categoryIds: categoryIds,
                vendorId: model.SearchVendorId,
                productType: model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null,
                keywords: model.SearchProductName,
                showHidden: true,
                overridePublished: overridePublished);

            try
            {
                var xml = _exportManager.ExportProductsToXml(products);

                return File(Encoding.UTF8.GetBytes(xml), MimeTypes.ApplicationXml, "products.xml");
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc);
                return RedirectToAction("List");
            }
        }

        [HttpPost]
        public virtual IActionResult ExportXmlSelected(string selectedIds)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            var products = new List<Product>();
            if (selectedIds != null)
            {
                var ids = selectedIds
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => Convert.ToInt32(x))
                    .ToArray();
                products.AddRange(_productService.GetProductsByIds(ids));
            }
            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null)
            {
                products = products.Where(p => p.VendorId == _workContext.CurrentVendor.Id).ToList();
            }

            var xml = _exportManager.ExportProductsToXml(products);

            return File(Encoding.UTF8.GetBytes(xml), MimeTypes.ApplicationXml, "products.xml");
        }

        [HttpPost, ActionName("List")]
        [FormValueRequired("exportexcel-all")]
        public virtual IActionResult ExportExcelAll(ProductSearchModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null)
            {
                model.SearchVendorId = _workContext.CurrentVendor.Id;
            }

            var categoryIds = new List<int> { model.SearchCategoryId };
            //include subcategories
            if (model.SearchIncludeSubCategories && model.SearchCategoryId > 0)
                categoryIds.AddRange(_categoryService.GetChildCategoryIds(parentCategoryId: model.SearchCategoryId, showHidden: true));

            //0 - all (according to "ShowHidden" parameter)
            //1 - published only
            //2 - unpublished only
            bool? overridePublished = null;
            if (model.SearchPublishedId == 1)
                overridePublished = true;
            else if (model.SearchPublishedId == 2)
                overridePublished = false;

            var products = _productService.SearchProducts(
                categoryIds: categoryIds,
                vendorId: model.SearchVendorId,
                productType: model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null,
                keywords: model.SearchProductName,
                showHidden: true,
                overridePublished: overridePublished);

            try
            {
                var bytes = _exportManager.ExportProductsToXlsx(products);

                return File(bytes, MimeTypes.TextXlsx, "products.xlsx");
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc);
                return RedirectToAction("List");
            }
        }

        [HttpPost]
        public virtual IActionResult ExportExcelSelected(string selectedIds)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            var products = new List<Product>();
            if (selectedIds != null)
            {
                var ids = selectedIds
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => Convert.ToInt32(x))
                    .ToArray();
                products.AddRange(_productService.GetProductsByIds(ids));
            }
            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null)
            {
                products = products.Where(p => p.VendorId == _workContext.CurrentVendor.Id).ToList();
            }

            var bytes = _exportManager.ExportProductsToXlsx(products);

            return File(bytes, MimeTypes.TextXlsx, "products.xlsx");
        }

        [HttpPost]
        public virtual IActionResult ImportExcel(IFormFile importexcelfile)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedView();

            if (_workContext.CurrentVendor != null && !_vendorSettings.AllowVendorsToImportProducts)
                //a vendor can not import products
                return AccessDeniedView();

            try
            {
                if (importexcelfile != null && importexcelfile.Length > 0)
                {
                    _importManager.ImportProductsFromXlsx(importexcelfile.OpenReadStream());
                }
                else
                {
                    _notificationService.ErrorNotification(_localizationService.GetResource("Admin.Common.UploadFile"));
                    return RedirectToAction("List");
                }

                _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Catalog.Products.Imported"));
                return RedirectToAction("List");
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc);
                return RedirectToAction("List");
            }
        }

        #endregion
        
        #endregion
    }
}