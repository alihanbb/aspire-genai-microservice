using AspireApps.BasketService.Services;
using AspireApps.ServiceDefaults.Messaging.Events;
using MassTransit;

namespace AspireApps.BasketService.EventHandlers
{
    public class ProductPriceChangedIntegrationEventHandler(BasketServices services)
        : IConsumer<ProductPriceChangedIntegrationEvent>
    {
        public async Task Consume(ConsumeContext<ProductPriceChangedIntegrationEvent> context)
        {
            await services.UpdateBasketItemProductPrices
                (context.Message.ProductId, (decimal)context.Message.Price);
        }
    }
}
