using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onion.Domain.Products;

namespace Onion.DataAccess.Configurations.EntityConfigurations
{
    public class ProductTypeConfiguration : IEntityTypeConfiguration<ProductType>
    {
        public void Configure(EntityTypeBuilder<ProductType> builder)
        {
            builder.ToTable("ProductTypes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .ValueGeneratedOnAdd();

            builder.Property(x => x.Description)
                   .HasMaxLength(100)
                   .IsRequired();
        }
    }
}
