using AspireApps.CatalogService.Services;
using Catalog.Services;

namespace AspireApps.CatalogService.Endpoint;

public  static class ProductEndpoint
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products");

        group.MapGet("/", async (ProductService service) =>
        {
            var product = await service.GetProductsAsync();
            return Results.Ok(product);
        })
        .WithName("GetAllProducts")
        .Produces<List<Product>>(StatusCodes.Status200OK);

        group.MapGet("/{id}", async (int id, ProductService service) =>
        {
            var anyProduct = await service.GetByIdAsync(id);
            if (anyProduct is null) return Results.NotFound();
            return Results.Ok(anyProduct);
        })
        .WithName("GetProductById")
        .Produces<Product>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", async (Product product, ProductService service) =>
        {
            await service.CreateProductAsynce(product);
            return Results.Created($"/products/{product.Id}", product);
        })
        .WithName("CreateProduct")
        .Produces<Product>(StatusCodes.Status201Created);

        group.MapPut("/{id}", async (int id, Product inputProduct, ProductService service) =>
        {
            var anyProduct = await service.GetByIdAsync(id);
            if (anyProduct is null) return Results.NotFound();
            await service.UpdateProduct(anyProduct, inputProduct);
            return Results.NoContent();
        })
        .WithName("UpdateProduct")
        .Produces<Product>(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}", async (int id, ProductService service) =>
        {
            var anyProduct = await service.GetByIdAsync(id);
            if (anyProduct is null) return Results.NotFound();
            await service.DeleteProduct(anyProduct);
            return Results.NoContent ();
        })
        .WithName("DeleteProduct")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/support/{query}", async(string query, ProductAIService service)=>
        {
            var response = await service.SupportAsync(query);
            return Results.Ok(response);
        })
        .WithName("Support")
        .Produces(StatusCodes.Status200OK);

        group.MapGet("/search/{query}", async (string query, ProductService service) =>
        {
            var products = await service.SearchProductsAsync(query);

            return Results.Ok(products);
        })
        .WithName("SearchProducts")
        .Produces<List<Product>>(StatusCodes.Status200OK);


        group.MapGet("aisearch/{query}", async (string query, ProductAIService service) =>
        {
            var products = await service.SearchProductsAsync(query);

            return Results.Ok(products);
        })
        .WithName("AISearchProducts")
        .Produces<List<Product>>(StatusCodes.Status200OK);

    }
}
