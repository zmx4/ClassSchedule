using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClassSchedule.Models;

public class ClassSchedule
{
    /// <summary>
    /// 主键
    /// </summary>
    public int Id{ get; init; }
    /// <summary>
    /// 名称
    /// </summary>
    /// <value>The name.</value>
    /// <remarks>The name of the class schedule.</remarks>
    [StringLength(64)]
    public string Name { get; init; } = string.Empty;
    public DateOnly StartTime { get; init; }
    public DateOnly EndTime { get; init; }
    /// <summary>
    /// 总周数
    /// </summary>
    /// <value>The total weeks.</value>
    /// <remarks>The total number of weeks in the class schedule.</remarks>
    public int TotalWeeks { get; init; } = 20;
    public ICollection<Class> Classes { get; init; } = new List<Class>();
}