﻿using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CyborgianStates.Models
{
    [Table("Permission")]
    public class Permission
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
    }
}
