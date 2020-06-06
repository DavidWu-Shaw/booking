using System;
using Nop.Core.Domain.Catalog;

namespace Nop.Web.Areas.Admin.Helpers
{
    public static class RecurringPaymentHelper
    {
        public static DateTime GetNexPaymentDate(DateTime startDate, DateTime currentDate, RecurringProductCyclePeriod cyclePeriod)
        {
            DateTime nextDate = currentDate.AddDays(1);
            switch (cyclePeriod)
            {
                case RecurringProductCyclePeriod.Months:
                    var months = 12 * (currentDate.Year - startDate.Year) + currentDate.Month - startDate.Month;
                    if (currentDate.Day >= startDate.Day)
                    {
                        months++;
                    }
                    nextDate = startDate.AddMonths(months);
                    break;
                case RecurringProductCyclePeriod.Years:
                    int years = (int)Math.Floor((currentDate - startDate).TotalDays / 365.25 + 1);
                    nextDate = startDate.AddYears(years);
                    break;
                case RecurringProductCyclePeriod.Weeks:
                    int weeks = (int)Math.Floor((currentDate - startDate).TotalDays / 7 + 1);
                    nextDate = startDate.AddDays(weeks * 7);
                    break;
            }

            return nextDate;
        }
    }
}
