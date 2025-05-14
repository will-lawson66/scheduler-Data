using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Instrument.Data.DataContext;
public class SchedulerDbContext : DbContext
{
    public SchedulerDbContext(DbContextOptions<SchedulerDbContext> options) 
        : base(options)
    {
    }

    public virtual DbSet<Sequence> Sequences { get; set; }
    public virtual DbSet<Parameter> Parameters { get; set; }
    public virtual DbSet<SequenceParameter> SequenceParameters { get; set; }
    public virtual DbSet<Entities.Range> Ranges { get; set; }
    public virtual DbSet<RangeValue> RangeValues { get; set; }
    public virtual DbSet<Resource> Resources { get; set; }
    public virtual DbSet<SequenceGroup> SequenceGroups { get; set; }
    public virtual DbSet<SequenceGroupSequence> SequenceGroupSequences { get; set; }
    public virtual DbSet<SequenceGroupCollectionBase> SequenceGroupCollections { get; set; }
    public virtual DbSet<SequenceGroupCollectionSequenceGroup> SequenceGroupCollectionSequenceGroups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Sequence>(entity =>
        {
            // Primary key configuration is now redundant with the [Key] attribute
            // but we'll keep it for clarity in the fluent API configuration
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);
        });
        
        modelBuilder.Entity<Parameter>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(e => e.DefaultValue)
                .HasMaxLength(1000);
                
            entity.Property(e => e.Min)
                .HasMaxLength(100);
                
            entity.Property(e => e.Max)
                .HasMaxLength(100);
                
            entity.Property(e => e.Format)
                .HasMaxLength(100);

            // Configure relationships
            entity.HasOne(e => e.Range)
                .WithMany(e => e.Parameters)
                .HasForeignKey(e => e.RangeId)
                .IsRequired(false);
                
            entity.HasOne(e => e.Resource)
                .WithMany(e => e.Parameters)
                .HasForeignKey(e => e.ResourceId)
                .IsRequired(false);
        });
        
        modelBuilder.Entity<SequenceParameter>(entity =>
        {
            // Configure the composite key - redundant with attributes but kept for clarity
            entity.HasKey(e => new { e.SequenceId, e.ParameterId });
            
            // The foreign key relationships are already configured with attributes
            // but we'll keep them in the fluent API for clarity and complete configuration
            entity.HasOne(e => e.Sequence)
                .WithMany(e => e.SequenceParameters)
                .HasForeignKey(e => e.SequenceId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Parameter)
                .WithMany(e => e.SequenceParameters)
                .HasForeignKey(e => e.ParameterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Entities.Range>(entity => 
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
        });

        modelBuilder.Entity<RangeValue>(entity => 
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.Value)
                .IsRequired()
                .HasMaxLength(1000);
                
            // Configure relationship
            entity.HasOne(e => e.Range)
                .WithMany(e => e.RangeValues)
                .HasForeignKey(e => e.RangeId);
        });

        modelBuilder.Entity<Resource>(entity => 
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50);
        });
        
        modelBuilder.Entity<SequenceGroup>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
        });
        
        modelBuilder.Entity<SequenceGroupSequence>(entity =>
        {
            // Configure the composite key
            entity.HasKey(e => new { e.SequenceGroupId, e.SequenceId });
            
            // Configure the many-to-many relationship with explicit cascade behavior
            entity.HasOne(e => e.SequenceGroup)
                .WithMany(e => e.SequenceGroupSequences)
                .HasForeignKey(e => e.SequenceGroupId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Sequence)
                .WithMany()
                .HasForeignKey(e => e.SequenceId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Configure the Order property
            entity.Property(e => e.Order)
                .IsRequired();
        });

        modelBuilder.Entity<SequenceGroupCollectionBase>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(c => c.CategoryTypeName)
                .IsRequired();

            entity.Property(c => c.CategoryName)
                .IsRequired();
        });

        // Add table-per-hierarchy mapping for SequenceGroupCollection<TEnum> 
        modelBuilder.Entity<SequenceGroupCollectionBase>()
            .HasDiscriminator<string>("CategoryTypeName")
            .HasValue<SequenceGroupCollection<Technology>>(typeof(Technology).FullName ?? string.Empty); //Technology collections
            // Add other enum types as needed 


        modelBuilder.Entity<SequenceGroupCollectionSequenceGroup>(entity =>
        {
            // Configure the composite key
            entity.HasKey(e => new { e.SequenceGroupCollectionId, e.SequenceGroupId });

            // Configure the many-to-many relationship with explicit cascade behavior
            entity.HasOne(e => e.SequenceGroupCollection)
                .WithMany(e => e.SequenceGroupCollectionSequenceGroups)
                .HasForeignKey(e => e.SequenceGroupCollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SequenceGroup)
                .WithMany()
                .HasForeignKey(e => e.SequenceGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure the Order property
            entity.Property(e => e.Order)
                .IsRequired();
        });
    }
}
