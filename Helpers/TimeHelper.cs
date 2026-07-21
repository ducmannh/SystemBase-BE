using System;

namespace SystemBase.BE.Helpers
{
    public static class TimeHelper
    {
        public static DateTime GetVietnamTime()
        {
            return DateTime.UtcNow.AddHours(7);
        }
    }
}
