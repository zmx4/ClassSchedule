using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using ClassSchedule.Models;
using HtmlAgilityPack;

namespace ClassSchedule.Services;

// 模型类型 ClassSchedule 与命名空间 ClassSchedule 冲突，使用别名区分
using Schedule = ClassSchedule.Models.ClassSchedule;

/// <summary>
/// 解析正方教务系统（ZFSoft）课表查询页面 HTML，
/// 数据集中在 <c>#kbgrid_table_0</c> 表格中。
/// </summary>
public static class ScheduleHtmlParser
{
    /// <summary>课程名末尾的课程类型标记：★理论 ▲实验 ◆实践。</summary>
    private const string CourseTypeMarkers = "★▲◆";

    private static readonly Regex SectionRangeRegex = new(@"(\d+)\s*[-~－—]\s*(\d+)\s*节", RegexOptions.Compiled);
    private static readonly Regex SemesterRegex = new(@"\d{4}-\d{4}学年第\d+学期", RegexOptions.Compiled);

    /// <summary>将课表页面 HTML 解析为课程表模型。</summary>
    public static Schedule Parse(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var table = document.DocumentNode.SelectSingleNode("//table[@id='kbgrid_table_0']")
                   ?? throw new InvalidOperationException("未找到 id 为 kbgrid_table_0 的课表表格。");

        var schedule = new Schedule
        {
            Name = ExtractSemester(table) ?? "未知学期",
        };

        // 按教学班名称(Code)归组，同一门课（如周一/周三都有）合并为一个 Class。
        var classByKey = new Dictionary<string, Class>(StringComparer.Ordinal);

        var cells = table.SelectNodes(".//td[contains(concat(' ', normalize-space(@class), ' '), ' td_wrap ')]");
        if (cells is null)
        {
            return schedule;
        }

        foreach (var cell in cells)
        {
            var id = cell.GetAttributeValue("id", string.Empty);
            // id 形如 "{星期}-{节次}"，例如 1-1 表示 星期一 第1节。
            if (!TryParseCellId(id, out var dayOfWeek, out var fallbackSection))
            {
                continue;
            }

            var courseDivs = cell.SelectNodes(".//div[contains(concat(' ', normalize-space(@class), ' '), ' timetable_con ')]");
            if (courseDivs is null)
            {
                continue;
            }

            foreach (var courseDiv in courseDivs)
            {
                var entry = ParseCourseEntry(courseDiv, dayOfWeek, fallbackSection);
                if (entry is null)
                {
                    continue;
                }

                var key = string.IsNullOrWhiteSpace(entry.Class.Code)
                    ? entry.Class.Name
                    : entry.Class.Code;
                if (!classByKey.TryGetValue(key, out var classItem))
                {
                    classItem = entry.Class;
                    classByKey.Add(key, classItem);
                    schedule.Classes.Add(classItem);
                }
                else
                {
                    classItem.ClassTimes.Add(entry.ClassTime);
                }
            }
        }

        return schedule;
    }

    private static CourseEntry? ParseCourseEntry(HtmlNode courseDiv, int dayOfWeek, int fallbackSection)
    {
        var titleNode = courseDiv.SelectSingleNode(".//span[contains(concat(' ', normalize-space(@class), ' '), ' title ')]");
        var name = CleanCourseName(titleNode?.InnerText);

        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        string instructor = string.Empty;
        string location = string.Empty;
        string code = string.Empty;
        double credits = 0;
        int startSection = fallbackSection;
        int endSection = fallbackSection;
        long weekMask = 0;

        var paragraphs = courseDiv.SelectNodes("./p");
        if (paragraphs is not null)
        {
            foreach (var paragraph in paragraphs)
            {
                var tooltip = paragraph.SelectSingleNode(".//span[@data-toggle='tooltip']");
                var kind = tooltip?.GetAttributeValue("title", string.Empty).Trim() ?? string.Empty;
                var text = paragraph.InnerText.Trim();

                switch (kind)
                {
                    case "节/周":
                        if (TryParseSectionRange(text, out var start, out var end))
                        {
                            startSection = start;
                            endSection = end;
                        }
                        weekMask = ParseWeekMask(StripSection(text));
                        break;
                    case "上课地点":
                        location = text;
                        break;
                    case "教师":
                        instructor = text;
                        break;
                    case "教学班名称":
                        code = text;
                        break;
                    case "学分":
                        credits = ParseCredits(text);
                        break;
                }
            }
        }

        var classItem = new Class
        {
            Name = name,
            Instructor = instructor,
            Location = location,
            Code = code,
            Credits = credits,
        };

        var classTime = new ClassTime
        {
            DayOfWeek = dayOfWeek,
            WeekBitmask = weekMask,
            StartTime = startSection,
            EndTime = endSection,
        };

        classItem.ClassTimes.Add(classTime);
        return new CourseEntry(classItem, classTime);
    }

    /// <summary>解析单元格 id，形如 "1-3"（星期1 第3节）。</summary>
    private static bool TryParseCellId(string id, out int dayOfWeek, out int section)
    {
        dayOfWeek = 0;
        section = 0;
        var parts = id.Split('-');
        if (parts.Length != 2)
        {
            return false;
        }
        if (!int.TryParse(parts[0], out dayOfWeek) || !int.TryParse(parts[1], out section))
        {
            return false;
        }
        return dayOfWeek is >= 1 and <= 7 && section >= 1;
    }

    /// <summary>去除课程名末尾的 ★/▲/◆ 类型标记。</summary>
    private static string CleanCourseName(string? raw)
    {
        var name = raw?.Trim() ?? string.Empty;
        return name.TrimEnd(CourseTypeMarkers.ToCharArray()).Trim();
    }

    /// <summary>解析 "(3-4节)" 形式的节次范围。</summary>
    private static bool TryParseSectionRange(string text, out int start, out int end)
    {
        var match = SectionRangeRegex.Match(text);
        if (match.Success
            && int.TryParse(match.Groups[1].Value, out start)
            && int.TryParse(match.Groups[2].Value, out end)
            && end >= start)
        {
            return true;
        }
        start = 0;
        end = 0;
        return false;
    }

    /// <summary>去掉节次部分，只保留周次描述，如 "(3-4节)1-8周" -> "1-8周"。</summary>
    private static string StripSection(string text)
    {
        var match = SectionRangeRegex.Match(text);
        return match.Success ? text[(match.Index + match.Length)..] : text;
    }

    /// <summary>
    /// 将周次描述转为位掩码。支持 "1-16"、"1,3,5"、"1-8,10-16"、"1-16(单)/(双)" 等格式。
    /// 位编号即周号（第1周对应第1位），与 ClassTimeExtension 保持一致。
    /// </summary>
    public static long ParseWeekMask(string weekText)
    {
        long mask = 0;
        var oddOnly = weekText.Contains('单');
        var evenOnly = weekText.Contains('双');

        // 仅保留数字、逗号、连接符
        var numberPart = Regex.Replace(weekText, @"[^0-9,\-，]", string.Empty);
        foreach (var rawPart in numberPart.Split(',', '，'))
        {
            var part = rawPart.Trim();
            if (part.Length == 0)
            {
                continue;
            }

            var dash = part.IndexOf('-');
            if (dash > 0)
            {
                if (int.TryParse(part[..dash], out var start) && int.TryParse(part[(dash + 1)..], out var end))
                {
                    for (var week = start; week <= end; week++)
                    {
                        mask |= 1L << week;
                    }
                }
            }
            else if (int.TryParse(part, out var single))
            {
                mask |= 1L << single;
            }
        }

        if (oddOnly || evenOnly)
        {
            long filtered = 0;
            for (var week = 1; week <= 60; week++)
            {
                if ((mask & (1L << week)) == 0)
                {
                    continue;
                }
                var isOdd = (week & 1) == 1;
                if ((oddOnly && isOdd) || (evenOnly && !isOdd))
                {
                    filtered |= 1L << week;
                }
            }
            mask = filtered;
        }

        return mask;
    }

    private static double ParseCredits(string text)
    {
        var match = Regex.Match(text, @"\d+(\.\d+)?");
        return match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var credits)
            ? credits
            : 0;
    }

    private static string? ExtractSemester(HtmlNode table)
    {
        var titleRow = table.SelectSingleNode(".//tr[1]");
        var text = titleRow?.InnerText ?? string.Empty;
        var match = SemesterRegex.Match(text);
        return match.Success ? match.Value : null;
    }

    private sealed record CourseEntry(Class Class, ClassTime ClassTime);
}
