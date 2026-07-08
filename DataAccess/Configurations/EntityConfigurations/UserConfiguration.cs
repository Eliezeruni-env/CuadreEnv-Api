using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onion.Domain.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace Onion.DataAccess.Configurations.EntityConfigurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.FirstName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.LastName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Identification)
                   .IsRequired()
                   .HasMaxLength(20);

            builder.Property(x => x.Gender)
                   .IsRequired()
                   .HasMaxLength(1);

            builder.Property(x => x.Email)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.PasswordHash)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.Property(x => x.PhoneNumber)
                   .HasMaxLength(20);

            builder.Property(x => x.UserName)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(x => x.BirthDate)
                   .HasColumnType("date");

            builder.HasIndex(x => x.Email)
                   .IsUnique();

            builder.HasIndex(x => x.UserName)
                   .IsUnique();

            builder.HasIndex(x => x.Identification)
                   .IsUnique();

            builder.Property(x => x.Active)
                   .HasDefaultValue(true);

            builder.Property(x => x.CreationDate)
                   .HasDefaultValueSql("GETDATE()");
        }
    }
}