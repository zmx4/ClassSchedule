using System.Collections.Generic;
using ClassSchedule.Models;

namespace ClassSchedule.Extensions;

static class ClassTimeExtension
{
    extension(ClassTime classTime)
    {
        public bool IsThisWeek(int weekNumber)
        {
            return (classTime.WeekBitmask & (1L << weekNumber)) != 0;
        }
        
        public int[] GetActiveWeeks()
        {
            var activeWeeks = new List<int>();
            for (int i = 0; i < 64; i++)
            {
                if (classTime.IsThisWeek(i))
                {
                    activeWeeks.Add(i);
                }
            }
            return activeWeeks.ToArray();
        }
        
        public void SetActiveWeeks(int[] weeks)
        {
            classTime.WeekBitmask = 0;
            foreach (var week in weeks)
            {
                classTime.WeekBitmask |= (1L << week);
            }
        }
    }
}