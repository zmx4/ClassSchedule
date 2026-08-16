using ClassSchedule.Models;
using Microsoft.EntityFrameworkCore;

namespace ClassSchedule.Data;

// 模型类型 ClassSchedule 与命名空间 ClassSchedule 冲突，使用别名区分
using Schedule = Models.ClassSchedule;

/// <summary>课程表数据库上下文，使用 SQLite 存储。</summary>
public class ClassScheduleDbContext(DbContextOptions<ClassScheduleDbContext> options) : DbContext(options)
{
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<ClassTime> ClassTimes => Set<ClassTime>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.ToTable("Schedules");
            entity.HasKey(e => e.Id);
            entity.HasMany(e => e.Classes)
                .WithOne()
                .HasForeignKey(e => e.ClassScheduleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.ToTable("Classes");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code);
            entity.HasMany(e => e.ClassTimes)
                .WithOne()
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClassTime>(entity =>
        {
            entity.ToTable("ClassTimes");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DayOfWeek, e.StartTime, e.EndTime });
        });
    }
}
