using Nop.Web.Areas.Admin.Models.Self;
using System;
using System.Collections.Generic;

namespace Nop.Web.Areas.Admin.Helpers
{
    public class AppointmentTimeSlotHelper
    {
        private int _morningStart;
        private int _morningEnd;
        private int _afternoonStart;
        private int _afternoonEnd;

        public AppointmentTimeSlotHelper(int morningStart, int morningEnd, int afternoonStart, int afternoonEnd)
        {
            _morningStart = morningStart;
            _morningEnd = morningEnd;
            _afternoonStart = afternoonStart;
            _afternoonEnd = afternoonEnd;
        }

        public List<TimeSlot> GetSlotsByHour(DateTime start, DateTime end)
        {
            var result = new List<TimeSlot>();
            // Number of days betwwen start and end
            double days = (end - start).TotalDays;
            for (int i = 0; i < days; i++)
            {
                DateTime midnightOfDay = start.AddDays(i).Date;
                for (int x = _morningStart; x < _morningEnd; x++)
                {
                    result.Add(new TimeSlot { Start = midnightOfDay.AddHours(x), End = midnightOfDay.AddHours(x + 1) });
                }
                for (int x = _afternoonStart; x < _afternoonEnd; x++)
                {
                    result.Add(new TimeSlot { Start = midnightOfDay.AddHours(x), End = midnightOfDay.AddHours(x + 1) });
                }
            }

            return result;
        }
    }
}
