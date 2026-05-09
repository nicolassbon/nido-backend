using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nido.Domain.Households;

namespace Nido.Infrastructure.Persistence.Configurations;

public sealed class HouseholdConfiguration : IEntityTypeConfiguration<Household>
{
    public void Configure(EntityTypeBuilder<Household> builder)
    {
        builder.ToTable("Household");

        builder.HasKey(household => household.Id);
        builder.Property(household => household.Id).ValueGeneratedNever();

        builder.OwnsOne(household => household.Name, nameBuilder =>
        {
            nameBuilder.Property(name => name.Value)
                .HasColumnName("Name")
                .IsRequired();
        });

        builder.Navigation(household => household.Name).IsRequired();
    }
}
