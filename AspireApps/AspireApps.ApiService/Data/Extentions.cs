namespace AspireApps.CatalogService.Data
{
    public static class Extentions
    {
        public static void UseMigration(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            // Primary database
            var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
            context.Database.Migrate();
            DataSeeder.Seed(context);

            // Read replica database - ensure schema exists (no migrations, uses EnsureCreated)
            var readContext = scope.ServiceProvider.GetRequiredService<ProductReadDbContext>();
            readContext.Database.EnsureCreated();
        }
    }
}
