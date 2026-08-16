namespace ClassSchedule.Models;

public class ClassTime
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    /// <summary>星期几：1=星期一 … 7=星期日</summary>
    public int DayOfWeek { get; set; }
    public long WeekBitmask { get; set; }
    public int StartTime { get; set; }
    public int EndTime { get; set; }
}