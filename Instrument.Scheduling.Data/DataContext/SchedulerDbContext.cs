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

    public DbSet<Sequence> Sequences{ get; set; }

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
    }
}
