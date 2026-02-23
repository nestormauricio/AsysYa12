using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProviderOptimizerService.Domain.Entities;
using ProviderOptimizerService.Domain.ValueObjects;

namespace ProviderOptimizerService.Infrastructure.Data.Configurations;

public class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("providers");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.PhoneNumber).HasMaxLength(20);
        builder.Property(p => p.Type).IsRequired();
        builder.Property(p => p.IsAvailable).IsRequired();
        builder.Property(p => p.Rating).IsRequired().HasPrecision(3, 2);
        builder.Property(p => p.ActiveAssignments).IsRequired();
        builder.Property(p => p.TotalAssignments).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
        builder.Property(p => p.RowVersion).IsRowVersion(); // optimistic concurrency (#33)
        builder.OwnsOne(p => p.Location, loc =>
        {
            loc.Property(g => g.Latitude).HasColumnName("latitude").IsRequired();
            loc.Property(g => g.Longitude).HasColumnName("longitude").IsRequired();
        });
        builder.Ignore(p => p.DomainEvents);
    }
}

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(200);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Role).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Ignore(u => u.DomainEvents);
    }
}

public class AssistanceRequestConfiguration : IEntityTypeConfiguration<AssistanceRequest>
{
    public void Configure(EntityTypeBuilder<AssistanceRequest> builder)
    {
        builder.ToTable("assistance_requests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RequestorName).IsRequired().HasMaxLength(100);
        builder.Property(r => r.RequiredType).IsRequired();
        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();
        builder.OwnsOne(r => r.RequestLocation, loc =>
        {
            loc.Property(g => g.Latitude).HasColumnName("latitude").IsRequired();
            loc.Property(g => g.Longitude).HasColumnName("longitude").IsRequired();
        });
        builder.HasOne(r => r.AssignedProvider).WithMany().HasForeignKey(r => r.AssignedProviderId);
        builder.Ignore(r => r.DomainEvents);
    }
}
