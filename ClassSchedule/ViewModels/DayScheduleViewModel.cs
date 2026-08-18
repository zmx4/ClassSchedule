using System.Collections.Generic;

namespace ClassSchedule.ViewModels;

/// <summary>课表网格中的一天（一列），包含定位好的课程项。</summary>
public sealed class DayScheduleViewModel
{
    /// <summary>星期名称，如 "周一"。</summary>
    public string DayName { get; init; } = string.Empty;

    /// <summary>星期几：1=周一 … 7=周日。</summary>
    public int DayOfWeek { get; init; }

    /// <summary>轨道列数（用于错开重叠课程）。</summary>
    public int TrackCount { get; init; } = 1;

    /// <summary>当天网格的全部项（分隔线在前，课程卡片在后）。</summary>
    public IReadOnlyList<ScheduleItemViewModel> Items { get; init; } = new List<ScheduleItemViewModel>();
}
