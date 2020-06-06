using Nop.Web.Framework.Models;
using Nop.Web.Models.Self;

namespace Nop.Web.Models.Catalog
{
    public partial class ProductDetailsModel : BaseNopEntityModel
    {
        //public AppointmentUpdateModel AppointmentUpdateModel { get; set; }
        public bool IsUserAuthenticated { get; set; }
        public string ManageCalendarUrl { get; set; }
    }
}
