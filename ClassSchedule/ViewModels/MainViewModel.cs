using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using ClassSchedule.Extensions;
using ClassSchedule.Models;
using ClassSchedule.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassSchedule.ViewModels;

// 模型类型 ClassSchedule 与命名空间 ClassSchedule 冲突，使用别名区分
using Schedule = ClassSchedule.Models.ClassSchedule;

/// <summary>主页面 ViewModel：加载课表、按周构建课表网格并驱动周选择滑动条。</summary>
public partial class MainViewModel : ViewModelBase
{
    private static readonly string[] DayNames = ["", "周一", "周二", "周三", "周四", "周五", "周六", "周日"];

    private static readonly Color[] Palette =
    [
        Color.Parse("#3B82F6"), Color.Parse("#0EA5E9"), Color.Parse("#10B981"),
        Color.Parse("#F59E0B"), Color.Parse("#EF4444"), Color.Parse("#8B5CF6"),
        Color.Parse("#14B8A6"), Color.Parse("#F97316")
    ];

    private readonly ScheduleRepository? _repository;
    private Schedule? _schedule;

    /// <summary>课表名称（学期）。</summary>
    [ObservableProperty]
    private string _scheduleName = "我的课表";

    /// <summary>当前选中的周（滑动条值，double 以匹配 Slider.Value）。</summary>
    [ObservableProperty]
    private double _selectedWeek = 1;

    /// <summary>总周数（滑动条上限）。</summary>
    [ObservableProperty]
    private double _totalWeeks = 20;

    /// <summary>周文本，如 "第 3 / 20 周"。</summary>
    [ObservableProperty]
    private string _weekText = "第 1 / 20 周";

    /// <summary>最大节次（决定课表行数）。</summary>
    [ObservableProperty]
    private int _maxSection = 12;

    /// <summary>是否有课表数据。</summary>
    [ObservableProperty]
    private bool _hasSchedule;

    /// <summary>是否无课表数据（空状态显示）。</summary>
    [ObservableProperty]
    private bool _hasNoSchedule = true;

    /// <summary>无课表数据时显示的提示。</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    partial void OnHasScheduleChanged(bool value)
    {
        HasNoSchedule = !value;
    }

    /// <summary>节次标签，如 "第1节"。</summary>
    [ObservableProperty]
    private IReadOnlyList<string> _sectionLabels = Array.Empty<string>();

    /// <summary>当前周 7 天的课表列。</summary>
    [ObservableProperty]
    private ObservableCollection<DayScheduleViewModel> _days = new();

    /// <summary>设计期或空仓储构造。</summary>
    public MainViewModel() : this(null) { }

    /// <summary>运行时构造，从仓储加载课表。</summary>
    public MainViewModel(ScheduleRepository? repository)
    {
        _repository = repository;
        if (repository is not null)
        {
            LoadFromRepository();
        }
        else if (Design.IsDesignMode)
        {
            _schedule = CreateSampleSchedule();
            ApplySchedule();
        }
        else
        {
            HasSchedule = false;
            StatusMessage = "暂无课表数据，请先导入一份课程表。";
        }
    }

    partial void OnSelectedWeekChanged(double value)
    {
        WeekText = $"第 {(int)Math.Round(value)} / {(int)TotalWeeks} 周";
        if (_schedule is not null)
        {
            RebuildWeekGrid();
        }
    }

    partial void OnTotalWeeksChanged(double value)
    {
        WeekText = $"第 {(int)Math.Round(SelectedWeek)} / {(int)value} 周";
    }

    private void LoadFromRepository()
    {
        var schedules = _repository!.GetAllSchedules();
        _schedule = schedules.LastOrDefault();
        if (_schedule is null)
        {
            // 首次运行数据库为空时，尝试导入随应用分发的示例课表用于演示；
            // 后续可替换为正式的“导入课表”功能。
            TrySeedFromSample();
            schedules = _repository.GetAllSchedules();
            _schedule = schedules.LastOrDefault();
        }

        if (_schedule is null)
        {
            HasSchedule = false;
            StatusMessage = "暂无课表数据，请先导入一份课程表。";
            return;
        }

        ApplySchedule();
    }

    /// <summary>当数据库为空时，尝试从输出目录的示例 HTML 导入一份课表。</summary>
    private void TrySeedFromSample()
    {
        try
        {
            var samplePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Resources", "个人课表查询.html");
            if (!System.IO.File.Exists(samplePath))
            {
                return;
            }

            var html = System.IO.File.ReadAllText(samplePath, System.Text.Encoding.UTF8);
            var parsed = ScheduleHtmlParser.Parse(html);
            if (parsed.Classes.Count > 0)
            {
                _repository!.SaveSchedule(parsed);
            }
        }
        catch
        {
            // 示例缺失或解析失败时静默忽略，页面进入空状态。
        }
    }

    private void ApplySchedule()
    {
        var schedule = _schedule!;
        ScheduleName = string.IsNullOrWhiteSpace(schedule.Name) ? "我的课表" : schedule.Name;
        TotalWeeks = Math.Max(1, schedule.TotalWeeks);
        SelectedWeek = CalculateCurrentWeek(schedule);
        BuildGrid();
    }

    private void BuildGrid()
    {
        var schedule = _schedule!;
        HasSchedule = true;
        StatusMessage = string.Empty;

        MaxSection = MaxSectionCount(schedule);
        SectionLabels = Enumerable.Range(1, MaxSection).Select(i => $"第{i}节").ToArray();

        RebuildWeekGrid();
    }

    private void RebuildWeekGrid()
    {
        Days.Clear();
        if (_schedule is null)
        {
            return;
        }

        var week = (int)Math.Round(SelectedWeek);
        foreach (var day in BuildDays(_schedule, week, MaxSection))
        {
            Days.Add(day);
        }
    }

    /// <summary>按选中的周构建 7 天的课表列。</summary>
    private static List<DayScheduleViewModel> BuildDays(Schedule schedule, int week, int maxSection)
    {
        var result = new List<DayScheduleViewModel>();
        for (var day = 1; day <= 7; day++)
        {
            var cards = new List<ScheduleItemViewModel>();
            foreach (var classItem in schedule.Classes)
            {
                foreach (var classTime in classItem.ClassTimes)
                {
                    if (classTime.DayOfWeek != day || !classTime.IsThisWeek(week))
                    {
                        continue;
                    }

                    cards.Add(new ScheduleItemViewModel
                    {
                        Name = classItem.Name,
                        Location = classItem.Location,
                        Instructor = classItem.Instructor,
                        SectionText = classTime.StartTime == classTime.EndTime
                            ? $"第{classTime.StartTime}节"
                            : $"第{classTime.StartTime}-{classTime.EndTime}节",
                        Row = classTime.StartTime - 1,
                        RowSpan = classTime.EndTime - classTime.StartTime + 1,
                        ColumnSpan = 1,
                        Color = PickBrush(classItem.Name),
                    });
                }
            }

            // 贪心分配轨道（子列），错开同一天重叠的课程。
            var tracks = new List<List<ScheduleItemViewModel>>();
            foreach (var card in cards.OrderBy(c => c.Row).ThenBy(c => c.RowSpan))
            {
                var trackIndex = 0;
                for (; trackIndex < tracks.Count; trackIndex++)
                {
                    var last = tracks[trackIndex][^1];
                    if (last.Row + last.RowSpan <= card.Row)
                    {
                        break;
                    }
                }

                if (trackIndex == tracks.Count)
                {
                    tracks.Add(new List<ScheduleItemViewModel>());
                }

                tracks[trackIndex].Add(card);
                card.Column = trackIndex;
            }

            var trackCount = Math.Max(1, tracks.Count);

            // 每节一行底边分隔线，铺满所有轨道，形成课表网格。
            var items = new List<ScheduleItemViewModel>();
            for (var section = 1; section <= maxSection; section++)
            {
                items.Add(new ScheduleItemViewModel
                {
                    IsGridLine = true,
                    Row = section - 1,
                    RowSpan = 1,
                    ColumnSpan = trackCount,
                });
            }
            items.AddRange(cards);

            result.Add(new DayScheduleViewModel
            {
                DayName = DayNames[day],
                DayOfWeek = day,
                TrackCount = trackCount,
                Items = items,
            });
        }

        return result;
    }

    /// <summary>计算课表内最大的节次号。</summary>
    private static int MaxSectionCount(Schedule schedule)
    {
        var max = 1;
        foreach (var classItem in schedule.Classes)
        {
            foreach (var classTime in classItem.ClassTimes)
            {
                if (classTime.EndTime > max)
                {
                    max = classTime.EndTime;
                }
            }
        }

        return max;
    }

    /// <summary>根据开学日期计算当前周，取整并限制在 [1, TotalWeeks]。</summary>
    private static int CalculateCurrentWeek(Schedule schedule)
    {
        if (schedule.StartTime.Year >= 2000)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var elapsedDays = today.DayNumber - schedule.StartTime.DayNumber;
            var week = elapsedDays / 7 + 1;
            if (week < 1)
            {
                week = 1;
            }

            if (week > schedule.TotalWeeks)
            {
                week = schedule.TotalWeeks;
            }

            return week;
        }

        return 1;
    }

    /// <summary>按课程名取一个稳定地配色。</summary>
    private static IBrush PickBrush(string name)
    {
        var hash = 0;
        foreach (var c in name)
        {
            hash = hash * 31 + c;
        }

        return new SolidColorBrush(Palette[(hash & 0x7FFFFFFF) % Palette.Length]);
    }

    /// <summary>设计期示例课表，便于预览主页面。</summary>
    private static Schedule CreateSampleSchedule()
    {
        var schedule = new Schedule
        {
            Name = "2026-2027学年第1学期",
            StartTime = DateOnly.FromDateTime(DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7))),
            TotalWeeks = 20,
        };

        AddClass(schedule, "软件工程", "3-410", "张老师", 1, 3, 4, RangeMask(1, 16));
        AddClass(schedule, "大型数据库技术", "2-305", "赵老师", 1, 6, 8, RangeMask(1, 16));
        AddClass(schedule, "数据结构", "1-208", "李老师", 2, 1, 2, RangeMask(1, 16));
        AddClass(schedule, "操作系统", "4-102", "王老师", 3, 3, 4, RangeMask(1, 16));
        AddClass(schedule, "计算机网络", "3-501", "陈老师", 4, 6, 7, RangeMask(1, 8));
        AddClass(schedule, "大学英语", "5-203", "刘老师", 5, 1, 2, RangeMask(1, 16));
        AddClass(schedule, "高等数学", "1-101", "孙老师", 6, 3, 4, RangeMask(1, 16));
        AddClass(schedule, "体育", "操场", "周老师", 7, 7, 8, RangeMask(1, 16));
        return schedule;
    }

    private static void AddClass(Schedule schedule, string name, string location, string instructor,
        int day, int start, int end, long mask)
    {
        var classItem = new Class
        {
            Name = name,
            Location = location,
            Instructor = instructor,
        };
        classItem.ClassTimes.Add(new ClassTime
        {
            DayOfWeek = day,
            StartTime = start,
            EndTime = end,
            WeekBitmask = mask,
        });
        schedule.Classes.Add(classItem);
    }

    private static long RangeMask(int start, int count)
    {
        long mask = 0;
        for (var w = start; w < start + count; w++)
        {
            mask |= 1L << w;
        }

        return mask;
    }
}