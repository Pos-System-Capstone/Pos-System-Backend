﻿using System;
using System.Collections.Generic;

namespace Pos_System.Domain.Models
{
    public partial class GroupVariant
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
