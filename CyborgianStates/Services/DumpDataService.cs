using CyborgianStates.Data.Models.Dump;
using CyborgianStates.Enums;
using CyborgianStates.Interfaces;
using NationStatesSharp.Enums;
using NationStatesSharp.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CyborgianStates.Services
{
    public class DumpDataService : IDumpDataService
    {
        private IDumpRetrievalService _dumpRetrievalService;
        private ILogger _logger;
        public DumpDataService(IDumpRetrievalService dumpRetrievalService)
        {
            _dumpRetrievalService = dumpRetrievalService;
            _logger = Log.Logger.ForContext<DumpDataService>();
            Status = DumpDataStatus.Empty;
        }
        public ImmutableHashSet<DumpRegion> Regions { get; private set; }

        public ImmutableHashSet<DumpNation> Nations { get; private set; }

        public DumpDataStatus Status { get; private set; }

        public Task UpdateAsync()
        {
            _logger.Information("--- Dump Data Update started ---");
            Status = DumpDataStatus.Updating;
            _logger.Information("Status switched to {Status}", Status);
            try
            {
                ReadRegions();
                ReadNations();
                Status = DumpDataStatus.Ready;
            }
            catch (Exception ex)
            {
                Status = DumpDataStatus.Error;
                _logger.Fatal(ex, "Dump Data Update failed.");
            }
            _logger.Information("Status switched to {Status}", Status);
            _logger.Information("--- Dump Data Update completed ---");
            return Task.CompletedTask;
        }

        private void ReadRegions()
        {
            var stopWatch = Stopwatch.StartNew();
            try
            {
                _logger.Debug("Extracting regions-dump stream to DumpRegion collection.");
                using (var fileStream = new FileStream(_dumpRetrievalService.GetDumpFileNameByDumpType(DumpType.Regions), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var stream = new GZipStream(fileStream, CompressionMode.Decompress))
                    {
                        var doc = XDocument.Load(stream);
                        Regions = doc.Root.Descendants().Where(e => e.Name == "REGION").AsParallel().Select(e =>
                        {
                            return new DumpRegion()
                            {
                                Name = Helpers.ToID(e.Element("NAME").Value),
                                UnescapedName = e.Element("NAME").Value,
                                NationNames = e.Element("NATIONS").Value.Split(":").ToHashSet(),
                                Delegate = e.Element("DELEGATE").Value,
                                Founder = e.Element("FOUNDER").Value,
                                Flag = e.Element("FLAG").Value
                            };
                        }).ToImmutableHashSet();
                    }
                }
                _logger.Debug("Parsing regions took {elapsed} to complete.", stopWatch.Elapsed);
            }
            finally
            {
                stopWatch.Stop();
            }
        }

        private void ReadNations()
        {
            var stopWatch = Stopwatch.StartNew();
            try
            {
                _logger.Debug("Extracting nations-dump stream to DumpRegion collection.");
                using (var fileStream = new FileStream(_dumpRetrievalService.GetDumpFileNameByDumpType(DumpType.Nations), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var stream = new GZipStream(fileStream, CompressionMode.Decompress))
                    {
                        var doc = XDocument.Load(stream);
                        Nations = doc.Root.Descendants()
                            .Where(e => e.Name == "NATION")
                            .AsParallel()
                            .Select(e =>
                        {
                            var nationName = e.Element("NAME").Value;
                            return new DumpNation()
                            {
                                Name = Helpers.ToID(nationName),
                                RegionName = Helpers.ToID(e.Element("REGION").Value),
                                UnescapedName = nationName,
                                IsWAMember = e.Element("UNSTATUS").Value is "WA Member" or "WA Delegate",
                                Endorsements = e.Element("ENDORSEMENTS").Value.Split(",").ToList(),
                                Influence = e.Element("INFLUENCE").Value
                            };
                        }).ToImmutableHashSet();
                    }
                }
                _logger.Debug("Parsing nations took {elapsed} to complete.", stopWatch.Elapsed);
            }
            finally
            {
                stopWatch.Stop();
            }
        }

        private DumpRegion GetRegionInternal(string name) => Regions.FirstOrDefault(r => r.Name == Helpers.ToID(name));
        private DumpNation GetNationInternal(string name) => Nations.FirstOrDefault(n => n.Name == Helpers.ToID(name));
        public DumpRegion GetRegionByName(string name) => GetRegionInternal(name);
        public DumpNation GetNationByName(string name) => GetNationInternal(name);
        public List<DumpNation> GetNationsByRegionName(string name) => Nations.Where(n => n.RegionName == Helpers.ToID(name)).ToList();
        public List<DumpNation> GetWANationsByRegionName(string name) => Nations.Where(n => n.RegionName == Helpers.ToID(name) && n.IsWAMember).ToList();
        public int GetEndoSumByRegionName(string name) => GetWANationsByRegionName(name).Sum(n => n.Endorsements.Count);

    }
}