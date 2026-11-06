using FleetDesk.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetDesk.Api.Data;

public class FleetDeskDbContext : DbContext
{
    public FleetDeskDbContext(DbContextOptions<FleetDeskDbContext> options) : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderPart> WorkOrderParts => Set<WorkOrderPart>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasIndex(v => v.PlateNumber).IsUnique();
            entity.Property(v => v.PlateNumber).HasMaxLength(16).IsRequired();
            entity.Property(v => v.Brand).HasMaxLength(60).IsRequired();
            entity.Property(v => v.Model).HasMaxLength(60).IsRequired();
            entity.Property(v => v.RowVersion).IsConcurrencyToken();
        });

        modelBuilder.Entity<Part>(entity =>
        {
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.Property(p => p.Sku).HasMaxLength(40).IsRequired();
            entity.Property(p => p.Name).HasMaxLength(120).IsRequired();
            entity.Property(p => p.UnitCost).HasColumnType("decimal(18,2)");
            entity.Property(p => p.RowVersion).IsConcurrencyToken();
        });

        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.Property(w => w.Description).HasMaxLength(500);
            entity.Property(w => w.MechanicUsername).HasMaxLength(60).IsRequired();
            entity.Property(w => w.RowVersion).IsConcurrencyToken();
            entity.HasOne(w => w.Vehicle)
                .WithMany(v => v.WorkOrders)
                .HasForeignKey(w => w.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkOrderPart>(entity =>
        {
            entity.Property(wp => wp.UnitCostAtUse).HasColumnType("decimal(18,2)");
            entity.HasOne(wp => wp.WorkOrder)
                .WithMany(w => w.Parts)
                .HasForeignKey(wp => wp.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(wp => wp.Part)
                .WithMany()
                .HasForeignKey(wp => wp.PartId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(60).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(30).IsRequired();
        });
    }
}
