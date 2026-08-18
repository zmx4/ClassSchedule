using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClassSchedule.Models;

public class Class
{
    public int Id{ get; init; }
    public int ClassScheduleId { get; init; }
    [StringLength(64)]
    public string Name { get; init; } = string.Empty;
    [StringLength(64)]
    public string Description { get; set; } = string.Empty;
    [StringLength(64)]
    public string Instructor { get; init; } = string.Empty;
    [StringLength(64)]
    public string Location { get; init; } = string.Empty;
    /// <summary>教学班名称/课程编号，如 (2026-2027-1)-BK20209-03</summary>
    [StringLength(64)]
    public string Code { get; init; } = string.Empty;

    /// <summary>学分</summary>
    public double Credits { get; init; }

    public ICollection<ClassTime> ClassTimes { get; init; } = new List<ClassTime>();
}