using AspireApps.BasketService.Entities;
using AspireApps.BasketService.Services;
namespace AspireApps.BasketService.Endpoints;

public static class BasketEndpoints
{
    public static void MapBasketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("basket")
            .RequireAuthorization(); // Keycloak JWT authentication required

        group.MapGet("/{userName}", async (string userName, BasketServices service) =>
        {
            var shoppingCart = await service.GetBasket(userName);
            if (shoppingCart is null) return Results.NotFound();
            return Results.Ok(shoppingCart);
        })
        .WithName("GetBasket")
        .Produces<ShoppingCart>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", async (ShoppingCart shoppingCart, BasketServices service) =>
        {
            await service.UpdateBasket(shoppingCart);
            return Results.Created("GetBasket", shoppingCart);

        })
        .WithName("UpdateBasket")
        .Produces<ShoppingCart>(StatusCodes.Status201Created);


        group.MapDelete("/{userName}", async(string userName, BasketServices service) =>
        {
            await service.DeleteBasket(userName);
            return Results.NoContent();
        })
        .WithName("DeleteBasket")
        .Produces(StatusCodes.Status204NoContent);

    }
}
