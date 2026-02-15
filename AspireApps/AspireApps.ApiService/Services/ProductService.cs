using AspireApps.CatalogService.Data;
using AspireApps.ServiceDefaults.Messaging.Events;
using MassTransit;

namespace AspireApps.CatalogService.Services;

public class ProductService(ProductDbContext productDbContext, IBus bus)
{
    public async Task CreateProductAsynce(Product product)
    {
        productDbContext.Products.Add(product);
        await productDbContext.SaveChangesAsync();
    }

    public async Task UpdateProduct(Product updatedProduct, Product inputProduct)
    {
        if(updatedProduct.Price  != inputProduct.Price)
        {
            var integrationEvent = new ProductPriceChangedIntegrationEvent
            {
                ProductId = updatedProduct.Id,
                Name = updatedProduct.Name,
                Price = inputProduct.Price,
                Description = updatedProduct.Description,
                ImageUrl = updatedProduct.ImageUrl
            };
            await bus.Publish(integrationEvent);
        }
        updatedProduct.Name = inputProduct.Name;
        updatedProduct.Description = inputProduct.Description;
        updatedProduct.ImageUrl = inputProduct.ImageUrl;
        updatedProduct.Price = inputProduct.Price;

        productDbContext.Products.Update(updatedProduct);
        await productDbContext.SaveChangesAsync();
    }

    public async Task DeleteProduct(Product product)
    {
        productDbContext.Remove(product);
        await productDbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsAsync()
    {
        return await productDbContext.Products.ToListAsync();
    }
    
    public async Task<Product?> GetByIdAsync(int id)
    {
        return await productDbContext.Products.FindAsync(id);
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(string query)
    {
        return await productDbContext.Products
            .Where(p => EF.Functions.ILike(p.Name, $"%{query}%") 
                     || EF.Functions.ILike(p.Description, $"%{query}%"))
            .ToListAsync();
    }
}