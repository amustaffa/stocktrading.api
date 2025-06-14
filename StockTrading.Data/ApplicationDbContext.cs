using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockTrading.Models.Domain;

namespace StockTrading.Data;
// Inherit from IdentityDbContext to include Identity tables
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Define DbSets for your application entities
    public DbSet<Stock> Stocks { get; set; } = null!;
    public DbSet<Trade> Trades { get; set; } = null!;
    public DbSet<Portfolio> Portfolios { get; set; } = null!;
    public DbSet<PortfolioItem> PortfolioItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure relationships and constraints here
        // Example: One-to-many relationship between Portfolio and PortfolioItem
        builder.Entity<PortfolioItem>()
            .HasOne(pi => pi.Portfolio)
            .WithMany(p => p.Items)
            .HasForeignKey(pi => pi.PortfolioId);

        // Configure relationship: Trade has one Stock
        builder.Entity<Trade>()
            .HasOne(t => t.Stock)
            .WithMany() // A Stock can be part of many trades
            .HasForeignKey(t => t.StockId);

        // Configure relationship: PortfolioItem has one Stock
        builder.Entity<PortfolioItem>()
            .HasOne(pi => pi.Stock)
            .WithMany() // A Stock can be part of many portfolio items
            .HasForeignKey(pi => pi.StockId);


        // Seed initial data for Stocks
        builder.Entity<Stock>().HasData(
            new Stock { Id = 1, Symbol = "AAPL", CompanyName = "Apple Inc.", CurrentPrice = 170.00m },
            new Stock { Id = 2, Symbol = "MSFT", CompanyName = "Microsoft Corp.", CurrentPrice = 250.00m },
            new Stock { Id = 3, Symbol = "GOOGL", CompanyName = "Alphabet Inc.", CurrentPrice = 120.00m },
            new Stock { Id = 4, Symbol = "AMZN", CompanyName = "Amazon.com Inc.", CurrentPrice = 100.00m }
        );
    }
}
