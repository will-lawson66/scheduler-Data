using Microsoft.EntityFrameworkCore;
using Instrument.Scheduling.Data.Entities;
using System.Text.Json;

namespace Instrument.Scheduling.Data.DataContext;
public class SchedulerDbContext : DbContext
{
    public SchedulerDbContext(DbContextOptions<SchedulerDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Sequence> Sequences { get; set; }
    public DbSet<Parameter> Parameters { get; set; }
    public DbSet<SequenceParameter> SequenceParameters { get; set; }

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
            
            entity.Property(e => e.ParameterType)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(e => e.DefaultValue)
                .HasMaxLength(1000);
                
            entity.Property(e => e.MinValue)
                .HasMaxLength(100);
                
            entity.Property(e => e.MaxValue)
                .HasMaxLength(100);
                
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
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
                
            entity.Property(e => e.OverrideValue)
                .HasMaxLength(1000);
        });
    }
}
