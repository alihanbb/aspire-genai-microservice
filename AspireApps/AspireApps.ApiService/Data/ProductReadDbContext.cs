namespace AspireApps.CatalogService.Data;


public class ProductReadDbContext : DbContext
{
    public ProductReadDbContext(DbContextOptions<ProductReadDbContext> options) : base(options) 
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }
    
    public DbSet<Product> Products => Set<Product>();

    public override int SaveChanges()
        => throw new InvalidOperationException("This is a read-only context. Write operations are not allowed.");

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("This is a read-only context. Write operations are not allowed.");
}
