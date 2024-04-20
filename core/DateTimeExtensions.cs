namespace CalmOnion.GAA;

public static class DateTimeExtensions
{
	public static DateTimeOffset StartOfWeek(this DateTimeOffset date, DayOfWeek startDay = DayOfWeek.Monday)
	{
		return date.AddDays(-(int)date.DayOfWeek + (int)startDay).StartOfDay();
	}

	public static DateTimeOffset StartOfDay(this DateTimeOffset date)
	{
		return date - date.TimeOfDay;
	}
}
