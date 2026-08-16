using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ClassSchedule.Services;

// 模型类型 ClassSchedule 与命名空间 ClassSchedule 冲突，使用别名区分
using Schedule = ClassSchedule.Models.ClassSchedule;

/// <summary>课程表的持久化仓储，负责将解析结果写入/读取 SQLite。</summary>
public class ScheduleRepository
{
    private readonly ClassScheduleDbContext _db;

    public ScheduleRepository(ClassScheduleDbContext db)
    {
        _db = db;
    }

    /// <summary>保存一份课程表（含课程及上课时间），返回新记录 Id。</summary>
    public int SaveSchedule(Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        _db.Schedules.Add(schedule);
        _db.SaveChanges();
        return schedule.Id;
    }

    /// <summary>按 Id 加载课程表，含 Classes 与 ClassTimes。</summary>
    public Schedule? GetScheduleById(int id)
    {
        return _db.Schedules
            .Include(s => s.Classes)
                .ThenInclude(c => c.ClassTimes)
            .FirstOrDefault(s => s.Id == id);
    }

    /// <summary>加载全部课程表。</summary>
    public List<Schedule> GetAllSchedules()
    {
        return _db.Schedules
            .Include(s => s.Classes)
                .ThenInclude(c => c.ClassTimes)
            .OrderBy(s => s.Id)
            .ToList();
    }

    /// <summary>删除指定课程表（级联删除 Classes 与 ClassTimes）。</summary>
    public bool DeleteSchedule(int id)
    {
        var schedule = _db.Schedules.Find(id);
        if (schedule is null)
        {
            return false;
        }
        _db.Schedules.Remove(schedule);
        _db.SaveChanges();
        return true;
    }
}
