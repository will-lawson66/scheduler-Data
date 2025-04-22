using Microsoft.EntityFrameworkCore;
using Scheduler.DataLayer.Entities;
using System.Text.Json;

namespace Scheduler.DataLayer.Data
{
    public class SchedulerDbContext : DbContext
    {
        public SchedulerDbContext(DbContextOptions<SchedulerDbContext> options) 
            : base(options)
        {
        }

        public DbSet<SequenceDefinition> SequenceDefinitions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SequenceDefinition>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .HasMaxLength(1000);

                // Convert Steps list to JSON
                entity.Property(e => e.Steps)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<List<SequenceStep>>(v, (JsonSerializerOptions)null));

                // Convert Parameters dictionary to JSON
                entity.Property(e => e.Parameters)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null));

                entity.Property(e => e.CreatedDate)
                    .IsRequired();
            });
        }
    }
}
