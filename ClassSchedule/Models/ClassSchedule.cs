using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClassSchedule.Models;

public class ClassSchedule
{
    public int Id{ get; init; }
    [StringLength(64)]
    public string Name { get; init; } = string.Empty;
    public DateOnly StartTime { get; init; }
    public DateOnly EndTime { get; init; }
    public ICollection<Class> Classes { get; init; } = new List<Class>();
}