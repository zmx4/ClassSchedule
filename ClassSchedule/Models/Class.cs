using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSchedule.Models;

public class Class
{
    public int Id{ get; set; }
    [ForeignKey(nameof(ClassSchedule))]
    public int ClassScheduleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public ICollection<ClassTime> ClassTimes { get; set; } = new List<ClassTime>();
}