using AspireApps.BasketService.ApiClients;
using AspireApps.BasketService.Entities;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace AspireApps.BasketService.Services;

public class BasketServices(IDistributedCache distributedCache, CatalogApiClient catalogApiClient)
{
    public async Task<ShoppingCart?> GetBasket(string userName)
    {
        var basket = await distributedCache.GetStringAsync(userName);
        return string.IsNullOrWhiteSpace(basket) ? null :
            JsonSerializer.Deserialize<ShoppingCart>(basket);
    }

    public async Task UpdateBasket(ShoppingCart shoppingCart)
    {
        foreach(var item in shoppingCart.Items)
        {
            var product = await catalogApiClient.GetProductById(item.ProductId);
            item.Price = product.Price;
            item.ProductName = product.Name;
        }

        await distributedCache.SetStringAsync(shoppingCart.UserName, JsonSerializer.Serialize(shoppingCart));
    }

    public async Task DeleteBasket(string userName)
    {
        await distributedCache.RemoveAsync(userName);
    }

    public async Task UpdateBasketItemProductPrices(int productId, decimal price)
    {
        var basket = await GetBasket("swn");
        var item = basket!.Items.FirstOrDefault(x => x.ProductId == productId);
        if(item is not null)
        {
            item.Price = price;
            await distributedCache.SetStringAsync(basket.UserName, JsonSerializer.Serialize(basket));
        }
    }
}
