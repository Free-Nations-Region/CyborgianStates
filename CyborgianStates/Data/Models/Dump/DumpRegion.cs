using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CyborgianStates.Data.Models.Dump
{
    public class DumpRegion
    {
        public string Name { get; set; }
        public string UnescapedName { get; set; }
        public string Founder { get; set; }
        public string Delegate { get; set; }
        public string Flag { get; set; }
        public HashSet<string> NationNames { get; set; }
    }
}