using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSchedule.Models;

public class ClassTime
{
    public int Id { get; set; }
    [ForeignKey(nameof(Class))]
    public int ClassId { get; set; }
    public long WeekBitmask { get; set; }
    public int StartTime { get; set; }
    public int EndTime { get; set; }
}