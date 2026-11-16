using Microsoft.EntityFrameworkCore;

namespace Abysalto.Cart.Api.Data;

public sealed class CartDbContext(DbContextOptions<CartDbContext> options)
    : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var products = modelBuilder.Entity<Product>();
        products.Property(x => x.Name).HasMaxLength(200);
        products.Property(x => x.UnitPrice).HasPrecision(18, 2);
        products.HasData(
            new Product { Id = 1, Name = "Product 1", UnitPrice = 9.99m },
            new Product { Id = 2, Name = "Product 2", UnitPrice = 2.01m });

        var carts = modelBuilder.Entity<Cart>();
        carts.Property(x => x.OwnerId).HasMaxLength(200);
        carts.Property(x => x.RowVersion).IsRowVersion();
        carts.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.CartId);

        var items = modelBuilder.Entity<CartItem>();
        items.HasKey(x => new { x.CartId, x.ProductId });
        items.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}