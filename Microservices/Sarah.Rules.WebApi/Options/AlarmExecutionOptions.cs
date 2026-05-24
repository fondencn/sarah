namespace Sarah.Rules.WebApi.Options;

public sealed class AlarmExecutionOptions
{
    public bool SummerModeEnabled { get; set; } = false;

    public bool SuppressTemperatureAlarmsDuringSummer { get; set; } = true;

    public int SummerStartMonth { get; set; } = 5;

    public int SummerEndMonth { get; set; } = 9;

    public bool IsSummer(DateTime nowLocal)
    {
        if (!SummerModeEnabled)
        {
            return false;
        }

        if (SummerStartMonth <= SummerEndMonth)
        {
            return nowLocal.Month >= SummerStartMonth && nowLocal.Month <= SummerEndMonth;
        }

        return nowLocal.Month >= SummerStartMonth || nowLocal.Month <= SummerEndMonth;
    }
}