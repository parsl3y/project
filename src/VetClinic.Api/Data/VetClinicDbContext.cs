using Microsoft.EntityFrameworkCore;
using VetClinic.Api.Entities;

namespace VetClinic.Api.Data;

public class VetClinicDbContext(DbContextOptions<VetClinicDbContext> options)
    : DbContext(options)
{
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Owner>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.FirstName).HasMaxLength(100).IsRequired();
            e.Property(o => o.LastName).HasMaxLength(100).IsRequired();
            e.Property(o => o.Phone).HasMaxLength(20).IsRequired();
            e.Property(o => o.Email).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Pet>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(100).IsRequired();
            e.Property(p => p.Breed).HasMaxLength(100);
            e.Property(p => p.Species).HasConversion<string>();
            e.HasOne(p => p.Owner)
             .WithMany(o => o.Pets)
             .HasForeignKey(p => p.OwnerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Appointment>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.VetName).HasMaxLength(100).IsRequired();
            e.Property(a => a.Reason).HasMaxLength(500);
            e.Property(a => a.Status).HasConversion<string>();
            e.HasOne(a => a.Pet)
             .WithMany(p => p.Appointments)
             .HasForeignKey(a => a.PetId)
             .OnDelete(DeleteBehavior.Cascade);

            // Бізнес-правило: одна тварина — один запис на дату
            e.HasIndex(a => new { a.PetId, a.Date }).IsUnique();
        });
    }
}