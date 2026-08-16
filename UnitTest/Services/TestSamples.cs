using System.Text;

namespace UnitTest.Services;

/// <summary>测试用共享数据与工具方法。</summary>
public static class TestSamples
{
    /// <summary>从测试输出目录加载示例课表 HTML。</summary>
    public static string LoadSampleHtml()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "个人课表查询.html");
        return File.ReadAllText(path, Encoding.UTF8);
    }

    /// <summary>根据周号数组生成位掩码（第 n 周对应第 n 位）。</summary>
    public static long Mask(params int[] weeks)
    {
        long mask = 0;
        foreach (var week in weeks)
        {
            mask |= 1L << week;
        }
        return mask;
    }

    /// <summary>生成 1..count 连续周号的掩码。</summary>
    public static long RangeMask(int start, int count)
    {
        long mask = 0;
        for (var w = start; w < start + count; w++)
        {
            mask |= 1L << w;
        }
        return mask;
    }
}
