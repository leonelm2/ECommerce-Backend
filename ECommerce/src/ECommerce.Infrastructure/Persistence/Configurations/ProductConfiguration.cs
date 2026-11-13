using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Description).HasMaxLength(500);
            builder.Property(p => p.Price).HasPrecision(18, 2).IsRequired();

            builder.Property(p => p.Stock).IsRequired().IsConcurrencyToken();

            builder.Property(p => p.CategoryId).IsRequired();
            builder.HasIndex(p => p.Name);
        }
    }
}