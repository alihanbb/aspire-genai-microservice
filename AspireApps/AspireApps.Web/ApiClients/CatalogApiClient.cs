using AspireApps.CatalogService.Entity;

namespace AspireApps.Web.ApiClients;

public class CatalogApiClient(HttpClient httpClient)
{
    public async Task<List<Product>> GetAllProductsAsync()
    {
        var response = await httpClient.GetFromJsonAsync<List<Product>>($"/products");
        return response;
    }

    public async Task<Product> GetProductByIdAsync(int id)
    {
        var response = await httpClient.GetFromJsonAsync<Product>($"/products/{id}");
        return response;
    }

    public async Task<string> SupportProducts(string query)
    {
        var response = await httpClient.GetStringAsync($"/products/support/{query}");
        return response;
    }

    public async Task<List<Product>?> SearchProducts(string query, bool aiSearch)
    {
        if (aiSearch)
        {
            return await httpClient.GetFromJsonAsync<List<Product>>($"/products/aisearch/{query}");
        }
        else
        {
            return await httpClient.GetFromJsonAsync<List<Product>>($"/products/search/{query}");
        }
    }

}
