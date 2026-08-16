using System.Collections.Generic;

namespace ClassSchedule.Models;

public class Class
{
    public int Id{ get; set; }
    public int ClassScheduleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    /// <summary>教学班名称/课程编号，如 (2026-2027-1)-BK20209-03</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>学分</summary>
    public double Credits { get; set; }
    public ICollection<ClassTime> ClassTimes { get; set; } = new List<ClassTime>();
}