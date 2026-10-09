using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onion.Domain.Products;

namespace Onion.DataAccess.Configurations.EntityConfigurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .ValueGeneratedOnAdd();

            builder.Property(x => x.Description)
                   .HasMaxLength(100)
                   .IsRequired();
            builder.Property(x => x.CompanyId)
                   .HasColumnType("int")
                   .HasDefaultValue(0)
                   .IsRequired();
            // Default creation metadata
            builder.Property(x => x.CreationDate)
                   .HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.CreateBy)
                   .HasMaxLength(200)
                   .HasDefaultValue("system");
            builder.Property(x => x.ModifiedBy)
                   .HasMaxLength(200)
                   .HasDefaultValue("system");
            builder.Property(x => x.Active)
                   .HasDefaultValue(true);
        }
    }
}
