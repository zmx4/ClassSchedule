using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClassSchedule.Models;

public class ClassSchedule
{
    public int Id{ get; set; }
    [StringLength(64)]
    public string Name { get; set; } = string.Empty;
    public DateOnly StartTime { get; set; }
    public DateOnly EndTime { get; set; }
    public ICollection<Class> Classes { get; set; } = new List<Class>();
}