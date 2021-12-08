using CyborgianStates.Interfaces;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CyborgianStates.Services
{
    public class BackgroundServiceRegistry : IBackgroundServiceRegistry
    {
        private List<IBackgroundService> _backgroundServices = new List<IBackgroundService>();
        private ISchedulerFactory _factory;
        private IScheduler _scheduler;

        public BackgroundServiceRegistry(ISchedulerFactory schedulerFactory)
        {
            _factory = schedulerFactory;
        }

        public void Register(IBackgroundService service) => _backgroundServices.Add(service);

        public async Task ShutdownAsync() => await _scheduler.Shutdown().ConfigureAwait(false);

        public async Task StartAsync()
        {
            _scheduler = await _factory.GetScheduler().ConfigureAwait(false);
            await _scheduler.Start().ConfigureAwait(false);
            foreach (var service in _backgroundServices)
            {
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity(service.Identity, service.Group)
                    .WithSchedule(CronScheduleBuilder.CronSchedule(service.CronSchedule).InTimeZone(service.TimeZone))
                    .ForJob(service.Identity, service.Group).Build();

                var type = service.GetType();
                IJobDetail job = JobBuilder.Create(type)
                    .WithIdentity(service.Identity, service.Group)
                    .Build();
                if (!await _scheduler.CheckExists(job.Key).ConfigureAwait(false))
                {
                    await _scheduler.ScheduleJob(job, trigger).ConfigureAwait(false);
                }
                if (service.DoStartNow)
                {
                    await _scheduler.TriggerJob(new JobKey(service.Identity, service.Group)).ConfigureAwait(false);
                }
            }
        }
    }
}