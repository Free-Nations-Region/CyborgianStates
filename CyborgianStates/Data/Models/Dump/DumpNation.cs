using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CyborgianStates.Data.Models.Dump
{
    public class DumpNation
    {
        public string Name { get; set; }
        public string RegionName { get; set; }
        public string UnescapedName { get; set; }
        public bool IsWAMember { get; set; }
        public string Influence { get; set; }
        public List<string> Endorsements { get; set; }
    }
}