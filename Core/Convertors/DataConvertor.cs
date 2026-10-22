using System;
using System.Globalization;

namespace Core.Convertors
{
    public static class DataConvertor
    {
        public static string ToShamci(this DateTime value)
        {
            PersianCalendar pc = new PersianCalendar();
            return pc.GetYear(value) + "/" + pc.GetMonth(value).ToString("00") + "/" + pc.GetDayOfMonth(value).ToString("00");

        }

    }
}
