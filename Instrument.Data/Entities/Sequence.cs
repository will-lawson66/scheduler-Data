namespace Instrument.Data.Entities;

public record Sequence
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required TimeSpan WorstCaseTime { get; init; }
    public bool CanBeParallel { get; init; }

    // Navigation property for the many-to-many relationship with Parameters
    public List<SequenceParameter> SequenceParameters { get; set; } = [];

    // Update method - returns a new instance with the specified changes
    public Sequence Update(string? name = null, TimeSpan? worstCaseTime = null, string? description = null, bool? canBeParallel = null)
    {
        // Validate updates if needed
        if (name != null && string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty", nameof(name));
        }

        // Use record's "with" expression for creating a modified copy
        return this with
        {
            Name = name ?? Name,
            WorstCaseTime = worstCaseTime ?? WorstCaseTime,
            Description = description ?? Description,
            CanBeParallel = canBeParallel ?? CanBeParallel
        };
    }
}