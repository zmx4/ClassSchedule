namespace ClassSchedule.Models;

public class ClassTime
{
    public int Id { get; init; }
    public int ClassId { get; init; }
    /// <summary>星期几：1=星期一 … 7=星期日</summary>
    public int DayOfWeek { get; init; }
    public long WeekBitmask { get; set; }
    public int StartTime { get; init; }
    public int EndTime { get; init; }
}