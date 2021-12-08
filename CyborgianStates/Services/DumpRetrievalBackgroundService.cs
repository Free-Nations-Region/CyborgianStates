using CyborgianStates.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NationStatesSharp.Enums;
using NationStatesSharp.Interfaces;
using Quartz;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace CyborgianStates.Services
{
    public class DumpRetrievalBackgroundService : IBackgroundService
    {
        private readonly IDumpRetrievalService _dumpRetrievalService;
        private readonly IDumpDataService _dumpDataService;
        private readonly ILogger _logger;
        private readonly AppSettings _settings;

        public DumpRetrievalBackgroundService() : this(Program.ServiceProvider)
        {
        }

        public DumpRetrievalBackgroundService(IServiceProvider serviceProvider)
        {
            _logger = Log.Logger.ForContext<DumpRetrievalBackgroundService>();
            _dumpRetrievalService = serviceProvider.GetRequiredService<IDumpRetrievalService>();
            _settings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;
            _dumpDataService = serviceProvider.GetRequiredService<IDumpDataService>();
        }

        public string Identity => "Dump Retrieval";

        public string Group => "Dump";

        public string CronSchedule => "0 30 22 ? * *";

        public TimeZoneInfo TimeZone => TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

        public bool DoStartNow => true;
        private bool _isRetrying = false;
        private bool? _successfullyUpdated = null;
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.Information("--- Dump Retrieval started ---");
                await RetrieveDumpAsync(DumpType.Nations, context).ConfigureAwait(false);
                await RetrieveDumpAsync(DumpType.Regions, context).ConfigureAwait(false);
                _logger.Information("--- Dump Retrieval completed ---");
                if (_successfullyUpdated ?? false)
                {
                    await _dumpDataService.UpdateAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Dump Retrieval failed with an unexpected fatal error.");
            }
        }

        private async Task RetrieveDumpAsync(DumpType dumpType, IJobExecutionContext context)
        {
            var fileName = Path.Combine(AppContext.BaseDirectory, _dumpRetrievalService.GetDumpFileNameByDumpType(dumpType));
            string existingHash = "";
            if (File.Exists(fileName))
            {
                _logger.Debug("Calculating hash from local file: {@fileName}", fileName);
                using (var fileStream = new FileStream(fileName, FileMode.Open))
                {
                    existingHash = await CalculateHashFromStreamAsync(fileStream).ConfigureAwait(false);
                }
            }
            if (!_dumpRetrievalService.IsLocalDumpAvailableAndUpToDate(dumpType))
            {
                var stopWatch = Stopwatch.StartNew();

                using (Stream responseStream = await _dumpRetrievalService.DownloadDumpAsync(dumpType, CancellationToken.None).ConfigureAwait(false))
                {
                    stopWatch.Stop();
                    _logger.Debug("Download {@dumpType} dump as stream took {@elapsed} to complete.", dumpType, stopWatch.Elapsed);
                    if (existingHash != await CalculateHashFromStreamAsync(responseStream).ConfigureAwait(false))
                    {
                        ArchiveDump(dumpType, fileName);
                        using (var fileStream = new FileStream(fileName, FileMode.Create))
                        {
                            stopWatch.Restart();
                            responseStream.Seek(0, SeekOrigin.Begin);
                            await responseStream.CopyToAsync(fileStream).ConfigureAwait(false);
                            stopWatch.Stop();
                            _logger.Debug("Writing {@dumpType} dump to local cache took {@elapsed} to complete.", dumpType, stopWatch.Elapsed);
                        }
                        _isRetrying = false;
                        //Set only to true if there is no value, if it is true no need to change, if it is false keep it at false
                        if (!_successfullyUpdated.HasValue)
                        {
                            _successfullyUpdated = true;
                        }
                    }
                    else
                    {
                        _logger.Warning("Downloaded {@dumpType} dump stream does not differ from local {@dumpType} dump. {@dumpType} dump was not updated yet.", dumpType);
                        await RerunJobAsync(context).ConfigureAwait(false);
                        _successfullyUpdated = false;
                    }
                }
            }
            else
            {
                _successfullyUpdated = true;
            }
        }

        private void ArchiveDump(DumpType dumpType, string fileName)
        {
            if (_settings.ArchiveDumps)
            {
                var archiveDir = Path.Combine(AppContext.BaseDirectory, "dump-archive", dumpType.ToString().ToLower());
                if (!Directory.Exists(archiveDir))
                {
                    Directory.CreateDirectory(archiveDir);
                }
                var info = new FileInfo(fileName);
                var archiveFilename = $"{info.LastWriteTimeUtc.Year}-{info.LastWriteTimeUtc.Month:00}-{info.LastWriteTimeUtc.Day:00}-{dumpType.ToString().ToLower()}.xml.gz";
                var targetFileName = Path.Combine(archiveDir, archiveFilename);
                _logger.Information("Archiving {@fileName} to {@archiveFileName}", fileName, targetFileName);
                if (File.Exists(fileName))
                {
                    File.Copy(fileName, targetFileName);
                }
                else
                {
                    _logger.Warning("File ({@fileName}) to archive not found.", fileName);
                }
            }
            else
            {
                _logger.Debug("{@dumpType} dump archiving skipped.", dumpType);
            }
        }

        private async Task<string> CalculateHashFromStreamAsync(Stream fileStream)
        {
            using (var hasher = SHA256.Create())
            {
                var bytes = await hasher.ComputeHashAsync(fileStream).ConfigureAwait(false);
                var hash = string.Concat(bytes.Select(b => b.ToString("x2")));
                _logger.Verbose("Hash: {@existingHash}", hash);
                return hash;
            }
        }

        private async Task RerunJobAsync(IJobExecutionContext context)
        {
            if (!_isRetrying)
            {
                var retryTrigger = TriggerBuilder.Create()
                    .ForJob(Identity, Group)
                    .StartAt(DateBuilder.FutureDate(30, IntervalUnit.Minute))
                    .Build();
                var rerun = await context.Scheduler.ScheduleJob(retryTrigger).ConfigureAwait(false);
                _isRetrying = true;
                _logger.Information("Dump retrieval will be retried at {@rerun}.", rerun.ToString("O"));
            }
        }
    }
}