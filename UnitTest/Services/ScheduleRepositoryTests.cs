using ClassSchedule.Data;
using ClassSchedule.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace UnitTest.Services;

public class ScheduleRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ClassScheduleDbContext> _options;

    public ScheduleRepositoryTests()
    {
        // 内存 SQLite，测试间相互隔离
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<ClassScheduleDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var db = new ClassScheduleDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private ClassScheduleDbContext CreateContext() => new(_options);

    [Fact]
    public void Save_解析结果可持久化到Sqlite并完整回读()
    {
        var parsed = ScheduleHtmlParser.Parse(TestSamples.LoadSampleHtml());

        int savedId;
        using (var db = CreateContext())
        {
            var repo = new ScheduleRepository(db);
            savedId = repo.SaveSchedule(parsed);
            Assert.True(savedId > 0);
        }

        using (var db = CreateContext())
        {
            var repo = new ScheduleRepository(db);
            var loaded = repo.GetScheduleById(savedId);

            Assert.NotNull(loaded);
            Assert.Equal("2026-2027学年第1学期", loaded.Name);
            Assert.Equal(11, loaded.Classes.Count);
            Assert.Equal(15, loaded.Classes.Sum(c => c.ClassTimes.Count));

            var bigData = Assert.Single(loaded.Classes, c => c.Name == "大型数据库技术");
            Assert.Equal("赵德玉", bigData.Instructor);
            Assert.Equal("(2026-2027-1)-BK20209-03", bigData.Code);
            Assert.Equal(2, bigData.ClassTimes.Count);

            var software = Assert.Single(loaded.Classes, c => c.Name == "软件工程");
            Assert.Equal(4, software.ClassTimes.Count);
        }
    }

    [Fact]
    public void GetAllSchedules_返回全部记录()
    {
        using var db = CreateContext();
        var repo = new ScheduleRepository(db);

        repo.SaveSchedule(ScheduleHtmlParser.Parse(TestSamples.LoadSampleHtml()));
        repo.SaveSchedule(ScheduleHtmlParser.Parse(TestSamples.LoadSampleHtml()));

        Assert.Equal(2, repo.GetAllSchedules().Count);
    }

    [Fact]
    public void Delete_级联删除课程与上课时间()
    {
        var parsed = ScheduleHtmlParser.Parse(TestSamples.LoadSampleHtml());

        using var db = CreateContext();
        var repo = new ScheduleRepository(db);
        var id = repo.SaveSchedule(parsed);

        Assert.True(repo.DeleteSchedule(id));
        Assert.Null(repo.GetScheduleById(id));
        Assert.Equal(0, db.Classes.Count());
        Assert.Equal(0, db.ClassTimes.Count());
    }

    [Fact]
    public void Delete_不存在的记录返回false()
    {
        using var db = CreateContext();
        var repo = new ScheduleRepository(db);
        Assert.False(repo.DeleteSchedule(999));
    }
}
