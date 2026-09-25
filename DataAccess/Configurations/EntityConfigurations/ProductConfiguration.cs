using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onion.Domain.Products;


namespace Onion.DataAccess.Configurations.EntityConfigurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .UseIdentityColumn()
                   .ValueGeneratedOnAdd();

            builder.Property(x => x.Description)
                   .HasMaxLength(200);

            builder.Property(x => x.Barcode)
                   .HasMaxLength(100);

            builder.Property(x => x.ShortDescription)
                   .HasMaxLength(100);

            builder.Property(x => x.Reference)
                   .HasMaxLength(50);

            // Store Cost as decimal in database with precision. Migrations will convert existing float columns to decimal(18,2).
            builder.Property(x => x.Cost)
                   .HasColumnType("decimal(18,2)");

            builder.Property(x => x.Stock)
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0);
            builder.Property(x => x.ReservedStock)
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0);
            builder.Property(x => x.RowVersion)
                   .IsRowVersion()
                   .IsConcurrencyToken();

            builder.Property(x => x.InvoiceWithoutStock)
                   .HasDefaultValue(false);

            builder.Property(x => x.IsOrganic)
                   .HasDefaultValue(false);

            builder.HasIndex(x => x.Barcode)
                   .IsUnique();

        }
    }
}
