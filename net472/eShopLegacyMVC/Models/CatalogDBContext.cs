using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace eShopLegacyMVC.Models
{
    public class CatalogDBContext : DbContext
    {
        public CatalogDBContext(DbContextOptions<CatalogDBContext> options) : base(options)
        {
        }

        public DbSet<CatalogItem> CatalogItems { get; set; } = null!;
        public DbSet<CatalogBrand> CatalogBrands { get; set; } = null!;
        public DbSet<CatalogType> CatalogTypes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureCatalogType(modelBuilder);
            ConfigureCatalogBrand(modelBuilder);
            ConfigureCatalogItem(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private void ConfigureCatalogType(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CatalogType>(entity =>
            {
                entity.ToTable("CatalogType");
                entity.HasKey(ci => ci.Id);
                entity.Property(ci => ci.Id).IsRequired();
                entity.Property(cb => cb.Type).IsRequired().HasMaxLength(100);
            });
        }

        private void ConfigureCatalogBrand(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CatalogBrand>(entity =>
            {
                entity.ToTable("CatalogBrand");
                entity.HasKey(ci => ci.Id);
                entity.Property(ci => ci.Id).IsRequired();
                entity.Property(cb => cb.Brand).IsRequired().HasMaxLength(100);
            });
        }

        private void ConfigureCatalogItem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CatalogItem>(entity =>
            {
                entity.ToTable("Catalog");
                entity.HasKey(ci => ci.Id);
                entity.Property(ci => ci.Id).ValueGeneratedNever().IsRequired();
                entity.Property(ci => ci.Name).IsRequired().HasMaxLength(50);
                entity.Property(ci => ci.Price).IsRequired();
                entity.Property(ci => ci.PictureFileName).IsRequired();
                entity.Ignore(ci => ci.PictureUri);

                entity.HasOne<CatalogBrand>(ci => ci.CatalogBrand)
                    .WithMany()
                    .HasForeignKey(ci => ci.CatalogBrandId)
                    .IsRequired();

                entity.HasOne<CatalogType>(ci => ci.CatalogType)
                    .WithMany()
                    .HasForeignKey(ci => ci.CatalogTypeId)
                    .IsRequired();
            });
        }
    }
}
