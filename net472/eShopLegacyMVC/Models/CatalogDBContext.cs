using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace eShopLegacyMVC.Models
{
    public class CatalogDBContext : DbContext
    {
        public CatalogDBContext(DbContextOptions<CatalogDBContext> options)
            : base(options)
        {
        }

        public DbSet<CatalogItem> CatalogItems { get; set; } = null!;
        public DbSet<CatalogBrand> CatalogBrands { get; set; } = null!;
        public DbSet<CatalogType> CatalogTypes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            ConfigureCatalogType(builder.Entity<CatalogType>());
            ConfigureCatalogBrand(builder.Entity<CatalogBrand>());
            ConfigureCatalogItem(builder.Entity<CatalogItem>());

            base.OnModelCreating(builder);
        }

        private static void ConfigureCatalogType(EntityTypeBuilder<CatalogType> entity)
        {
            entity.ToTable("CatalogType");
            entity.HasKey(ci => ci.Id);
            entity.Property(ci => ci.Id).IsRequired();
            entity.Property(cb => cb.Type).IsRequired().HasMaxLength(100);
        }

        private static void ConfigureCatalogBrand(EntityTypeBuilder<CatalogBrand> entity)
        {
            entity.ToTable("CatalogBrand");
            entity.HasKey(ci => ci.Id);
            entity.Property(ci => ci.Id).IsRequired();
            entity.Property(cb => cb.Brand).IsRequired().HasMaxLength(100);
        }

        private static void ConfigureCatalogItem(EntityTypeBuilder<CatalogItem> entity)
        {
            entity.ToTable("Catalog");
            entity.HasKey(ci => ci.Id);

            entity.Property(ci => ci.Id)
                .ValueGeneratedNever()
                .IsRequired();

            entity.Property(ci => ci.Name)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(ci => ci.Price).IsRequired();
            entity.Property(ci => ci.PictureFileName).IsRequired();

            entity.Ignore(ci => ci.PictureUri);

            entity.HasOne(ci => ci.CatalogBrand)
                .WithMany()
                .HasForeignKey(ci => ci.CatalogBrandId);

            entity.HasOne(ci => ci.CatalogType)
                .WithMany()
                .HasForeignKey(ci => ci.CatalogTypeId);
        }
    }
}
