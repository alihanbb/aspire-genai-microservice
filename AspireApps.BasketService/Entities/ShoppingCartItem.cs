namespace AspireApps.BasketService.Entities;

public class ShoppingCartItem
{
    public int ProductId { get; set; } = default!;
    public string Color { get; set; } = default!;
    public int Quantity { get; set; } = default!;
    public decimal Price { get; set; }
    public string ProductName { get; set; } = default!;
}