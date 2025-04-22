using System;
using System.Collections.Generic;

namespace Instrument.Scheduling.Data.Entities
{
    public class SequenceDefinition
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public List<SequenceStep> Steps { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class SequenceStep
    {
        public string Id { get; set; } = default!;
        public string Type { get; set; } = default!;
        public int Order { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}
