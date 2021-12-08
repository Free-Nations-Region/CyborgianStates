using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyborgianStates.Interfaces
{
    public interface IBackgroundService : IJob
    {
        string Identity { get; }
        string Group { get; }
        string CronSchedule { get; }
        TimeZoneInfo TimeZone { get; }
        bool DoStartNow { get; }
    }
}
