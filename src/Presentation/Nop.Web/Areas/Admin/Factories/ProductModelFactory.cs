using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Infrastructure.Cache;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework.Extensions;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;

namespace Nop.Web.Areas.Admin.Factories
{
    /// <summary>
    /// Represents the product model factory implementation
    /// </summary>
    public partial class ProductModelFactory : IProductModelFactory
    {
        #region Fields

        private readonly CatalogSettings _catalogSettings;
        private readonly CurrencySettings _currencySettings;
        private readonly IAclSupportedModelFactory _aclSupportedModelFactory;
        private readonly IBaseAdminModelFactory _baseAdminModelFactory;
        private readonly ICategoryService _categoryService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ILocalizationService _localizationService;
        private readonly ILocalizedModelFactory _localizedModelFactory;
        private readonly IPictureService _pictureService;
        private readonly IProductService _productService;
        private readonly IProductTagService _productTagService;
        private readonly IProductTemplateService _productTemplateService;
        private readonly ISettingModelFactory _settingModelFactory;
        private readonly IStaticCacheManager _cacheManager;
        private readonly IStoreMappingSupportedModelFactory _storeMappingSupportedModelFactory;
        private readonly IStoreService _storeService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWorkContext _workContext;
        private readonly TaxSettings _taxSettings;
        private readonly VendorSettings _vendorSettings;

        #endregion

        #region Ctor

        public ProductModelFactory(CatalogSettings catalogSettings,
            CurrencySettings currencySettings,
            IAclSupportedModelFactory aclSupportedModelFactory,
            IBaseAdminModelFactory baseAdminModelFactory,
            ICategoryService categoryService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IDateTimeHelper dateTimeHelper,
            ILocalizationService localizationService,
            ILocalizedModelFactory localizedModelFactory,
            IManufacturerService manufacturerService,
            IPictureService pictureService,
            IProductAttributeFormatter productAttributeFormatter,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IProductService productService,
            IProductTagService productTagService,
            IProductTemplateService productTemplateService,
            ISettingModelFactory settingModelFactory,
            ISpecificationAttributeService specificationAttributeService,
            IStaticCacheManager cacheManager,
            IStoreMappingSupportedModelFactory storeMappingSupportedModelFactory,
            IStoreService storeService,
            IUrlRecordService urlRecordService,
            IWorkContext workContext,
            TaxSettings taxSettings,
            VendorSettings vendorSettings)
        {
            _catalogSettings = catalogSettings;
            _currencySettings = currencySettings;
            _aclSupportedModelFactory = aclSupportedModelFactory;
            _baseAdminModelFactory = baseAdminModelFactory;
            _cacheManager = cacheManager;
            _categoryService = categoryService;
            _currencyService = currencyService;
            _customerService = customerService;
            _dateTimeHelper = dateTimeHelper;
            _localizationService = localizationService;
            _localizedModelFactory = localizedModelFactory;
            _pictureService = pictureService;
            _productService = productService;
            _productTagService = productTagService;
            _productTemplateService = productTemplateService;
            _settingModelFactory = settingModelFactory;
            _storeMappingSupportedModelFactory = storeMappingSupportedModelFactory;
            _storeService = storeService;
            _urlRecordService = urlRecordService;
            _workContext = workContext;
            _taxSettings = taxSettings;
            _vendorSettings = vendorSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Prepare copy product model
        /// </summary>
        /// <param name="model">Copy product model</param>
        /// <param name="product">Product</param>
        /// <returns>Copy product model</returns>
        protected virtual CopyProductModel PrepareCopyProductModel(CopyProductModel model, Product product)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.Id = product.Id;
            model.Name = string.Format(_localizationService.GetResource("Admin.Catalog.Products.Copy.Name.New"), product.Name);
            model.Published = true;
            model.CopyImages = true;

            return model;
        }

        protected virtual ProductScheduleModel PrepareProductScheduleModel(ProductScheduleModel model, Product product)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.BusinessBeginsHour = 9;
            model.BusinessEndsHour = 21;
            model.BusinessMorningShiftEndsHour = 12;
            model.BusinessAfternoonShiftBeginsHour = 13;
            model.BusinessOnWeekends = true;

            return model;
        }        

        /// <summary>
        /// Prepare related product search model
        /// </summary>
        /// <param name="searchModel">Related product search model</param>
        /// <param name="product">Product</param>
        /// <returns>Related product search model</returns>
        protected virtual RelatedProductSearchModel PrepareRelatedProductSearchModel(RelatedProductSearchModel searchModel, Product product)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            if (product == null)
                throw new ArgumentNullException(nameof(product));

            searchModel.ProductId = product.Id;

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        /// <summary>
        /// Prepare product picture search model
        /// </summary>
        /// <param name="searchModel">Product picture search model</param>
        /// <param name="product">Product</param>
        /// <returns>Product picture search model</returns>
        protected virtual ProductPictureSearchModel PrepareProductPictureSearchModel(ProductPictureSearchModel searchModel, Product product)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            if (product == null)
                throw new ArgumentNullException(nameof(product));

            searchModel.ProductId = product.Id;

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare product search model
        /// </summary>
        /// <param name="searchModel">Product search model</param>
        /// <returns>Product search model</returns>
        public virtual ProductSearchModel PrepareProductSearchModel(ProductSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //a vendor should have access only to his products
            searchModel.IsLoggedInAsVendor = _workContext.CurrentVendor != null;
            searchModel.AllowVendorsToImportProducts = _vendorSettings.AllowVendorsToImportProducts;

            //prepare available categories
            _baseAdminModelFactory.PrepareCategories(searchModel.AvailableCategories);

            //prepare available vendors
            _baseAdminModelFactory.PrepareVendors(searchModel.AvailableVendors);

            //prepare available product types
            _baseAdminModelFactory.PrepareProductTypes(searchModel.AvailableProductTypes);

            //prepare "published" filter (0 - all; 1 - published only; 2 - unpublished only)
            searchModel.AvailablePublishedOptions.Add(new SelectListItem
            {
                Value = "0",
                Text = _localizationService.GetResource("Admin.Catalog.Products.List.SearchPublished.All")
            });
            searchModel.AvailablePublishedOptions.Add(new SelectListItem
            {
                Value = "1",
                Text = _localizationService.GetResource("Admin.Catalog.Products.List.SearchPublished.PublishedOnly")
            });
            searchModel.AvailablePublishedOptions.Add(new SelectListItem
            {
                Value = "2",
                Text = _localizationService.GetResource("Admin.Catalog.Products.List.SearchPublished.UnpublishedOnly")
            });

            //prepare grid
            searchModel.SetGridPageSize();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged product list model
        /// </summary>
        /// <param name="searchModel">Product search model</param>
        /// <returns>Product list model</returns>
        public virtual ProductListModel PrepareProductListModel(ProductSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get parameters to filter comments
            var overridePublished = searchModel.SearchPublishedId == 0 ? null : (bool?)(searchModel.SearchPublishedId == 1);
            if (_workContext.CurrentVendor != null)
                searchModel.SearchVendorId = _workContext.CurrentVendor.Id;
            var categoryIds = new List<int> { searchModel.SearchCategoryId };
            if (searchModel.SearchIncludeSubCategories && searchModel.SearchCategoryId > 0)
            {
                var childCategoryIds = _categoryService.GetChildCategoryIds(parentCategoryId: searchModel.SearchCategoryId, showHidden: true);
                categoryIds.AddRange(childCategoryIds);
            }

            //get products
            var products = _productService.SearchProducts(showHidden: true,
                categoryIds: categoryIds,
                vendorId: searchModel.SearchVendorId,
                productType: searchModel.SearchProductTypeId > 0 ? (ProductType?)searchModel.SearchProductTypeId : null,
                keywords: searchModel.SearchProductName,
                pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize,
                overridePublished: overridePublished);

            //prepare list model
            var model = new ProductListModel().PrepareToGrid(searchModel, products, () =>
            {
                return products.Select(product =>
                {
                    //fill in model values from the entity
                    var productModel = product.ToModel<ProductModel>();

                    //little performance optimization: ensure that "FullDescription" is not returned
                    productModel.FullDescription = string.Empty;

                    //fill in additional values (not existing in the entity)
                    productModel.SeName = _urlRecordService.GetSeName(product, 0, true, false);
                    var defaultProductPicture = _pictureService.GetPicturesByProductId(product.Id, 1).FirstOrDefault();
                    productModel.PictureThumbnailUrl = _pictureService.GetPictureUrl(defaultProductPicture, 75);

                    return productModel;
                });
            });

            return model;
        }

        /// <summary>
        /// Prepare product model
        /// </summary>
        /// <param name="model">Product model</param>
        /// <param name="product">Product</param>
        /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
        /// <returns>Product model</returns>
        public virtual ProductModel PrepareProductModel(ProductModel model, Product product, bool excludeProperties = false)
        {
            Action<ProductLocalizedModel, int> localizedModelConfiguration = null;

            if (product != null)
            {
                //fill in model values from the entity
                if (model == null)
                {
                    model = product.ToModel<ProductModel>();
                    model.SeName = _urlRecordService.GetSeName(product, 0, true, false);
                }

                model.ProductTags = string.Join(", ", _productTagService.GetAllProductTagsByProductId(product.Id).Select(tag => tag.Name));

                if (!excludeProperties)
                {
                    model.SelectedCategoryIds = _categoryService.GetProductCategoriesByProductId(product.Id, true)
                        .Select(productCategory => productCategory.CategoryId).ToList();
                }

                //prepare copy product model
                PrepareCopyProductModel(model.CopyProductModel, product);
                PrepareProductScheduleModel(model.ProductScheduleModel, product);

                //prepare nested search model
                PrepareRelatedProductSearchModel(model.RelatedProductSearchModel, product);
                PrepareProductPictureSearchModel(model.ProductPictureSearchModel, product);

                //define localized model configuration action
                localizedModelConfiguration = (locale, languageId) =>
                {
                    locale.Name = _localizationService.GetLocalized(product, entity => entity.Name, languageId, false, false);
                    locale.FullDescription = _localizationService.GetLocalized(product, entity => entity.FullDescription, languageId, false, false);
                    locale.ShortDescription = _localizationService.GetLocalized(product, entity => entity.ShortDescription, languageId, false, false);
                    locale.MetaKeywords = _localizationService.GetLocalized(product, entity => entity.MetaKeywords, languageId, false, false);
                    locale.MetaDescription = _localizationService.GetLocalized(product, entity => entity.MetaDescription, languageId, false, false);
                    locale.MetaTitle = _localizationService.GetLocalized(product, entity => entity.MetaTitle, languageId, false, false);
                    locale.SeName = _urlRecordService.GetSeName(product, languageId, false, false);
                };
            }

            //set default values for the new model
            if (product == null)
            {
                model.AllowCustomerReviews = true;
                model.Published = true;
            }

            model.IsLoggedInAsVendor = _workContext.CurrentVendor != null;

            //prepare localized models
            if (!excludeProperties)
                model.Locales = _localizedModelFactory.PrepareLocalizedModels(localizedModelConfiguration);

            //prepare available vendors
            _baseAdminModelFactory.PrepareVendors(model.AvailableVendors,
                defaultItemText: _localizationService.GetResource("Admin.Catalog.Products.Fields.Vendor.None"));

            //prepare model categories
            _baseAdminModelFactory.PrepareCategories(model.AvailableCategories, false);
            foreach (var categoryItem in model.AvailableCategories)
            {
                categoryItem.Selected = int.TryParse(categoryItem.Value, out var categoryId)
                    && model.SelectedCategoryIds.Contains(categoryId);
            }

            //prepare model customer roles
            _aclSupportedModelFactory.PrepareModelCustomerRoles(model, product, excludeProperties);

            //prepare model stores
            _storeMappingSupportedModelFactory.PrepareModelStores(model, product, excludeProperties);

            return model;
        }

        /// <summary>
        /// Prepare paged related product list model
        /// </summary>
        /// <param name="searchModel">Related product search model</param>
        /// <param name="product">Product</param>
        /// <returns>Related product list model</returns>
        public virtual RelatedProductListModel PrepareRelatedProductListModel(RelatedProductSearchModel searchModel, Product product)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            if (product == null)
                throw new ArgumentNullException(nameof(product));

            //get related products
            var relatedProducts = _productService
                .GetRelatedProductsByProductId1(productId1: product.Id, showHidden: true).ToPagedList(searchModel);

            //prepare grid model
            var model = new RelatedProductListModel().PrepareToGrid(searchModel, relatedProducts, () =>
            {
                return relatedProducts.Select(relatedProduct =>
                {
                    //fill in model values from the entity
                    var relatedProductModel = relatedProduct.ToModel<RelatedProductModel>();

                    //fill in additional values (not existing in the entity)
                    relatedProductModel.Product2Name = _productService.GetProductById(relatedProduct.ProductId2)?.Name;

                    return relatedProductModel;
                });
            });
            return model;
        }

        /// <summary>
        /// Prepare related product search model to add to the product
        /// </summary>
        /// <param name="searchModel">Related product search model to add to the product</param>
        /// <returns>Related product search model to add to the product</returns>
        public virtual AddRelatedProductSearchModel PrepareAddRelatedProductSearchModel(AddRelatedProductSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            searchModel.IsLoggedInAsVendor = _workContext.CurrentVendor != null;

            //prepare available categories
            _baseAdminModelFactory.PrepareCategories(searchModel.AvailableCategories);

            //prepare available stores
            _baseAdminModelFactory.PrepareStores(searchModel.AvailableStores);

            //prepare available vendors
            _baseAdminModelFactory.PrepareVendors(searchModel.AvailableVendors);

            //prepare available product types
            _baseAdminModelFactory.PrepareProductTypes(searchModel.AvailableProductTypes);

            //prepare page parameters
            searchModel.SetPopupGridPageSize();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged related product list model to add to the product
        /// </summary>
        /// <param name="searchModel">Related product search model to add to the product</param>
        /// <returns>Related product list model to add to the product</returns>
        public virtual AddRelatedProductListModel PrepareAddRelatedProductListModel(AddRelatedProductSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null)
                searchModel.SearchVendorId = _workContext.CurrentVendor.Id;

            //get products
            var products = _productService.SearchProducts(showHidden: true,
                categoryIds: new List<int> { searchModel.SearchCategoryId },
                manufacturerId: searchModel.SearchManufacturerId,
                storeId: searchModel.SearchStoreId,
                vendorId: searchModel.SearchVendorId,
                productType: searchModel.SearchProductTypeId > 0 ? (ProductType?)searchModel.SearchProductTypeId : null,
                keywords: searchModel.SearchProductName,
                pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

            //prepare grid model
            var model = new AddRelatedProductListModel().PrepareToGrid(searchModel, products, () =>
            {
                return products.Select(product =>
                {
                    var productModel = product.ToModel<ProductModel>();
                    productModel.SeName = _urlRecordService.GetSeName(product, 0, true, false);

                    return productModel;
                });
            });

            return model;
        }

        /// <summary>
        /// Prepare paged product picture list model
        /// </summary>
        /// <param name="searchModel">Product picture search model</param>
        /// <param name="product">Product</param>
        /// <returns>Product picture list model</returns>
        public virtual ProductPictureListModel PrepareProductPictureListModel(ProductPictureSearchModel searchModel, Product product)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            if (product == null)
                throw new ArgumentNullException(nameof(product));

            //get product pictures
            var productPictures = _productService.GetProductPicturesByProductId(product.Id).ToPagedList(searchModel);

            //prepare grid model
            var model = new ProductPictureListModel().PrepareToGrid(searchModel, productPictures, () =>
            {
                return productPictures.Select(productPicture =>
                {
                    //fill in model values from the entity
                    var productPictureModel = productPicture.ToModel<ProductPictureModel>();

                    //fill in additional values (not existing in the entity)
                    var picture = _pictureService.GetPictureById(productPicture.PictureId)
                                  ?? throw new Exception("Picture cannot be loaded");

                    productPictureModel.PictureUrl = _pictureService.GetPictureUrl(picture);
                    productPictureModel.OverrideAltAttribute = picture.AltAttribute;
                    productPictureModel.OverrideTitleAttribute = picture.TitleAttribute;

                    return productPictureModel;
                });
            });

            return model;
        }

        /// <summary>
        /// Prepare product tag search model
        /// </summary>
        /// <param name="searchModel">Product tag search model</param>
        /// <returns>Product tag search model</returns>
        public virtual ProductTagSearchModel PrepareProductTagSearchModel(ProductTagSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return searchModel;
        }

        /// <summary>
        /// Prepare paged product tag list model
        /// </summary>
        /// <param name="searchModel">Product tag search model</param>
        /// <returns>Product tag list model</returns>
        public virtual ProductTagListModel PrepareProductTagListModel(ProductTagSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get product tags
            var productTags = _productTagService.GetAllProductTags()
                .OrderByDescending(tag => _productTagService.GetProductCount(tag.Id, storeId: 0, showHidden: true)).ToList()
                .ToPagedList(searchModel);

            //prepare list model
            var model = new ProductTagListModel().PrepareToGrid(searchModel, productTags, () =>
            {
                return productTags.Select(tag =>
                {
                    //fill in model values from the entity
                    var productTagModel = tag.ToModel<ProductTagModel>();

                    //fill in additional values (not existing in the entity)
                    productTagModel.ProductCount = _productTagService.GetProductCount(tag.Id, storeId: 0, showHidden: true);

                    return productTagModel;
                });
            });

            return model;
        }

        /// <summary>
        /// Prepare product tag model
        /// </summary>
        /// <param name="model">Product tag model</param>
        /// <param name="productTag">Product tag</param>
        /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
        /// <returns>Product tag model</returns>
        public virtual ProductTagModel PrepareProductTagModel(ProductTagModel model, ProductTag productTag, bool excludeProperties = false)
        {
            Action<ProductTagLocalizedModel, int> localizedModelConfiguration = null;

            if (productTag != null)
            {
                //fill in model values from the entity
                if (model == null)
                {
                    model = productTag.ToModel<ProductTagModel>();
                }

                model.ProductCount = _productTagService.GetProductCount(productTag.Id, storeId: 0, showHidden: true);

                //define localized model configuration action
                localizedModelConfiguration = (locale, languageId) =>
                {
                    locale.Name = _localizationService.GetLocalized(productTag, entity => entity.Name, languageId, false, false);
                };
            }

            //prepare localized models
            if (!excludeProperties)
                model.Locales = _localizedModelFactory.PrepareLocalizedModels(localizedModelConfiguration);

            return model;
        }

        #endregion
    }
}