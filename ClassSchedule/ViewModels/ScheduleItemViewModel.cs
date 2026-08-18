using Avalonia.Media;

namespace ClassSchedule.ViewModels;

/// <summary>课表网格中的一项：一门课程的卡片或一条网格分隔线，已包含定位信息。</summary>
public sealed class ScheduleItemViewModel
{
    /// <summary>是否为网格分隔线（而非课程卡片）。</summary>
    public bool IsGridLine { get; init; }

    /// <summary>0-based 行号（第 1 节对应 0）。</summary>
    public int Row { get; init; }

    /// <summary>跨行数（课程占用的节数）。</summary>
    public int RowSpan { get; init; }

    /// <summary>所在列：课程为当天内的轨道索引；分隔线为 0。</summary>
    public int Column { get; set; }

    /// <summary>跨列数：课程为 1；分隔线为当天轨道总数。</summary>
    public int ColumnSpan { get; init; }

    /// <summary>课程名。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>上课地点。</summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>教师。</summary>
    public string Instructor { get; init; } = string.Empty;

    /// <summary>节次文本，如 "第3-4节"。</summary>
    public string SectionText { get; init; } = string.Empty;

    /// <summary>卡片背景色。</summary>
    public IBrush Color { get; init; } = new SolidColorBrush(Avalonia.Media.Color.Parse("#3B82F6"));

    /// <summary>是否为课程卡片（用于切换显示）。</summary>
    public bool IsCourse => !IsGridLine;
}
