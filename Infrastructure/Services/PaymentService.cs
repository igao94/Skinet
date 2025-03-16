using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly ICartService _cartService;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(IConfiguration config, ICartService cartService, IUnitOfWork unitOfWork)
    {
        _cartService = cartService;
        _unitOfWork = unitOfWork;
        StripeConfiguration.ApiKey = config["StripeSettings:SecretKey"];
    }

    public async Task<ShoppingCart?> CreateOrUpdatePaymentIntentAsync(string cartId)
    {
        var cart = await _cartService.GetCartAsync(cartId);

        if (cart is null) return null;

        var shippingPrice = 0m;

        if (cart.DeliveryMethodId.HasValue)
        {
            var deliveryMethod = await _unitOfWork.Repository<DeliveryMethod>()
                .GetByIdAsync((int)cart.DeliveryMethodId);

            if (deliveryMethod is null) return null;

            shippingPrice = deliveryMethod.Price;
        }

        foreach (var item in cart.Items)
        {
            var productItem = await _unitOfWork.Repository<Core.Entities.Product>().GetByIdAsync(item.ProductId);

            if (productItem is null) return null;

            if (item.Price != productItem.Price) item.Price = productItem.Price;
        }

        var service = new PaymentIntentService();

        PaymentIntent? intent = null;

        if (string.IsNullOrEmpty(cart.PaymentIntentId))
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)cart.Items.Sum(x => x.Quantity * (x.Price * 100)) + (long)shippingPrice * 100,
                Currency = "usd",
                PaymentMethodTypes = ["card"]
            };

            intent = await service.CreateAsync(options);

            cart.PaymentIntentId = intent.Id;

            cart.ClientSecret = intent.ClientSecret;
        }
        else
        {
            var options = new PaymentIntentUpdateOptions
            {
                Amount = (long)cart.Items.Sum(x => x.Quantity * (x.Price * 100)) + (long)shippingPrice * 100
            };

            intent = await service.UpdateAsync(cart.PaymentIntentId, options);
        }

        await _cartService.SetCartAsync(cart);

        return cart;
    }

    public async Task<string> RefundPaymentAsync(string paymentIntentId)
    {
        var refundOptions = new RefundCreateOptions()
        {
            PaymentIntent = paymentIntentId,
        };

        var refundService = new RefundService();

        var result = await refundService.CreateAsync(refundOptions);

        return result.Status;
    }
}
