using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOCC.IBE.Compare.Models.Common
{
    public static class TimeUtils
    {
        public static TimeSpan? ToTimeSpan(int? time, TimeUnits? unit)
        {
            if (!time.HasValue || !unit.HasValue) return null;
            return unit switch
            {
                TimeUnits.Seconds => TimeSpan.FromSeconds(time.Value),
                TimeUnits.Minutes => TimeSpan.FromMinutes(time.Value),
                TimeUnits.Hours => TimeSpan.FromHours(time.Value),
                TimeUnits.Days => TimeSpan.FromDays(time.Value),
                TimeUnits.Weeks => TimeSpan.FromDays(time.Value * 7),
                TimeUnits.Months => TimeSpan.FromDays(time.Value * 30),
                _ => null
            };
        }

        //public static TimeSpan? ToTimeSpan(int? time, string unit)
        //{
        //    if (!time.HasValue || unit.IsNullOrWhiteSpace()) return null;

        //    return Enum.TryParse<TimeUnits>(unit, out var parsedUnit)
        //        ? ToTimeSpan(time, parsedUnit)
        //        : null;
        //}
    }
}
