using ClassSchedule.Services;

namespace UnitTest.Services;

// 模型类型 ClassSchedule 与命名空间 ClassSchedule 冲突，使用别名区分
using Schedule = ClassSchedule.Models.ClassSchedule;

public class ScheduleHtmlParserTests
{
    private static Schedule ParseSample() => ScheduleHtmlParser.Parse(TestSamples.LoadSampleHtml());

    [Fact]
    public void Parse_提取学期名称()
    {
        var schedule = ParseSample();
        Assert.Equal("2026-2027学年第1学期", schedule.Name);
    }

    [Fact]
    public void Parse_解析出全部课程与上课时间()
    {
        var schedule = ParseSample();
        Assert.Equal(11, schedule.Classes.Count);
        Assert.Equal(15, schedule.Classes.Sum(c => c.ClassTimes.Count));
    }

    [Fact]
    public void Parse_课程名去除类型标记()
    {
        var schedule = ParseSample();
        Assert.All(schedule.Classes, c => Assert.DoesNotContain("★▲◆", c.Name));
        Assert.Contains(schedule.Classes, c => c.Name == "大型数据库技术");
        Assert.Contains(schedule.Classes, c => c.Name == "软件设计与体系结构实验");
        Assert.Contains(schedule.Classes, c => c.Name == "形势与政策5");
    }

    [Fact]
    public void Parse_大型数据库技术_字段与上课时间正确()
    {
        var schedule = ParseSample();
        var cls = Assert.Single(schedule.Classes, c => c.Name == "大型数据库技术");

        Assert.Equal("赵德玉", cls.Instructor);
        Assert.Contains("信息中心207", cls.Location);
        Assert.Equal("(2026-2027-1)-BK20209-03", cls.Code);
        Assert.Equal(3.0, cls.Credits);

        // 周一、周三各上一次，均为 1-2 节、1-16 周
        Assert.Equal(2, cls.ClassTimes.Count);
        Assert.Equal(new[] { 1, 3 }, cls.ClassTimes.Select(t => t.DayOfWeek).OrderBy(d => d).ToArray());
        Assert.All(cls.ClassTimes, t =>
        {
            Assert.Equal(1, t.StartTime);
            Assert.Equal(2, t.EndTime);
            Assert.Equal(TestSamples.RangeMask(1, 16), t.WeekBitmask);
        });
    }

    [Fact]
    public void Parse_软件工程_按教学班合并并保留各周段()
    {
        var schedule = ParseSample();
        var cls = Assert.Single(schedule.Classes, c => c.Name == "软件工程");

        Assert.Equal("(2026-2027-1)-BK20019-05", cls.Code);
        // 周一 3-4 节（1-8 周、9-16 周）与 周四 3-4 节（1-8 周、9-16 周）
        Assert.Equal(4, cls.ClassTimes.Count);

        var monEarly = Assert.Single(cls.ClassTimes,
            t => t.DayOfWeek == 1 && t.WeekBitmask == TestSamples.RangeMask(1, 8));
        Assert.Equal(3, monEarly.StartTime);
        Assert.Equal(4, monEarly.EndTime);

        var monLate = Assert.Single(cls.ClassTimes,
            t => t.DayOfWeek == 1 && t.WeekBitmask == TestSamples.RangeMask(9, 8));
        Assert.Equal(3, monLate.StartTime);
        Assert.Equal(4, monLate.EndTime);

        var thuLate = Assert.Single(cls.ClassTimes,
            t => t.DayOfWeek == 4 && t.WeekBitmask == TestSamples.RangeMask(9, 8));
        Assert.Equal(3, thuLate.StartTime);
        Assert.Equal(4, thuLate.EndTime);
    }

    [Fact]
    public void Parse_形势与政策5_周段为9到12周()
    {
        var schedule = ParseSample();
        var cls = Assert.Single(schedule.Classes, c => c.Name == "形势与政策5");
        var time = Assert.Single(cls.ClassTimes);
        Assert.Equal(4, time.DayOfWeek);
        Assert.Equal(5, time.StartTime);
        Assert.Equal(6, time.EndTime);
        Assert.Equal(TestSamples.RangeMask(9, 4), time.WeekBitmask);
    }

    [Fact]
    public void Parse_科学艺术与创新_周五晚上()
    {
        var schedule = ParseSample();
        var cls = Assert.Single(schedule.Classes, c => c.Name == "科学艺术与创新");
        var time = Assert.Single(cls.ClassTimes);
        Assert.Equal(5, time.DayOfWeek);
        Assert.Equal(9, time.StartTime);
        Assert.Equal(10, time.EndTime);
        Assert.Equal(TestSamples.RangeMask(1, 8), time.WeekBitmask);
    }

    [Theory]
    [InlineData("1-16周", "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16")]
    [InlineData("1,3,5周", "1,3,5")]
    [InlineData("1-8,10-16周", "1,2,3,4,5,6,7,8,10,11,12,13,14,15,16")]
    [InlineData("9-12周", "9,10,11,12")]
    [InlineData("1-16周(单)", "1,3,5,7,9,11,13,15")]
    [InlineData("1-16周(双)", "2,4,6,8,10,12,14,16")]
    public void ParseWeekMask_解析各种周次格式(string input, string expectedCsv)
    {
        var expected = expectedCsv
            .Split(',')
            .Select(int.Parse)
            .ToArray();
        Assert.Equal(TestSamples.Mask(expected), ScheduleHtmlParser.ParseWeekMask(input));
    }
}
