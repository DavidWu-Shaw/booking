using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Catalog
{
    /// <summary>
    /// Represents a product picture model
    /// </summary>
    public partial class ProductScheduleModel : BaseNopEntityModel
    {
        #region Properties

        public int ProductId { get; set; }

        public int BusinessBeginsHour { get; set; }
        public int BusinessEndsHour { get; set; }
        public int BusinessMorningShiftEndsHour { get; set; }
        public int BusinessAfternoonShiftBeginsHour { get; set; }
        public bool BusinessOnWeekends { get; set; }

        #endregion
    }
}