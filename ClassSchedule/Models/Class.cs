using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClassSchedule.Models;

public class Class
{
    public int Id{ get; set; }
    public int ClassScheduleId { get; set; }
    [StringLength(64)]
    public string Name { get; set; } = string.Empty;
    [StringLength(64)]
    public string Description { get; set; } = string.Empty;
    [StringLength(64)]
    public string Instructor { get; set; } = string.Empty;
    [StringLength(64)]
    public string Location { get; set; } = string.Empty;
    /// <summary>教学班名称/课程编号，如 (2026-2027-1)-BK20209-03</summary>
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;
    /// <summary>学分</summary>
    public double Credits { get; set; }
    public ICollection<ClassTime> ClassTimes { get; set; } = new List<ClassTime>();
}