using WinUI3Localizer;

namespace MediaMaster.Extensions;

public static class DateTimeExtensions
{
    public static string GetTimeDifference(this DateTime date)
    {
        const int second = 1;
        const int minute = 60 * second;
        const int hour = 60 * minute;
        const int day = 24 * hour;
        const int month = 30 * day;

        var ts = DateTime.Now - date;
        var delta = ts.TotalSeconds;

        switch (delta)
        {
            case < 1 * minute:
                return ts.Seconds == 1 ? "/DateTime/one_second_ago".GetLocalizedString() : string.Format("/DateTime/seconds_ago".GetLocalizedString(), ts.Seconds);

            case < 2 * minute:
                return "/DateTime/a_minute_ago".GetLocalizedString();

            case < 60 * minute:
                return string.Format("/DateTime/minutes_ago".GetLocalizedString(), ts.Minutes);

            case < 120 * minute:
                return "/DateTime/an_hour_ago".GetLocalizedString();

            case < 24 * hour:
                return string.Format("/DateTime/hours_ago".GetLocalizedString(), ts.Hours);

            case < 48 * hour:
                return "/DateTime/yesterday".GetLocalizedString();

            case < 30 * day:
                return string.Format("/DateTime/days_ago".GetLocalizedString(), ts.Days);

            case < 12 * month:
            {
                var months = ts.Days / 30;
                return months <= 1 ? "/DateTime/one_month_ago".GetLocalizedString() : string.Format("/DateTime/months_ago".GetLocalizedString(), months);
            }

            default:
            {
                var years = ts.Days / 365;
                return years <= 1 ? "/DateTime/one_year_ago".GetLocalizedString() : string.Format("/DateTime/years_ago".GetLocalizedString(), years);
            }
        }
    }

}
