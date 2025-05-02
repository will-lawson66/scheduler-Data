using Microsoft.EntityFrameworkCore;
using Instrument.Data.Entities;
using System.Text.Json;

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
    public virtual DbSet<SequenceGroupSequences> SequenceGroupSequences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Sequence>(entity =>
        {
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
            // Configure the composite key
            entity.HasKey(e => new { e.SequenceId, e.ParameterId });
            
            // Configure the many-to-many relationship
            entity.HasOne(e => e.Sequence)
                .WithMany(e => e.SequenceParameters)
                .HasForeignKey(e => e.SequenceId);
                
            entity.HasOne(e => e.Parameter)
                .WithMany(e => e.SequenceParameters)
                .HasForeignKey(e => e.ParameterId);
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
                .WithMany(e => e.Values)
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
        
        modelBuilder.Entity<SequenceGroupSequences>(entity =>
        {
            // Configure the composite key
            entity.HasKey(e => new { e.SequenceGroupId, e.SequenceId });
            
            // Configure the many-to-many relationship
            entity.HasOne(e => e.SequenceGroup)
                .WithMany(e => e.SequenceGroupSequences)
                .HasForeignKey(e => e.SequenceGroupId);
                
            entity.HasOne(e => e.Sequence)
                .WithMany()
                .HasForeignKey(e => e.SequenceId);
                
            // Configure the Order property
            entity.Property(e => e.Order)
                .IsRequired();
        });
    }
}
