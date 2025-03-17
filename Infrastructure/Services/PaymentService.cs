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
        var cart = await _cartService.GetCartAsync(cartId)
            ?? throw new Exception("Cart unavailable.");

        var shippingPrice = await GetShippingPriceAsync(cart) ?? 0;

        await ValidateCartItemsInCartAsync(cart);

        var subtotal = CalculateSubtotal(cart);

        if (cart.AppCoupon is not null)
            subtotal = await ApplyDiscountAsync(cart.AppCoupon, subtotal);

        var total = subtotal + shippingPrice;

        await CreateUpdatePaymentIntentAsync(cart, total);

        return await _cartService.SetCartAsync(cart);
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

    private static async Task CreateUpdatePaymentIntentAsync(ShoppingCart cart, long total)
    {
        var service = new PaymentIntentService();

        if (string.IsNullOrEmpty(cart.PaymentIntentId))
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = total,
                Currency = "usd",
                PaymentMethodTypes = ["card"]
            };

            var intent = await service.CreateAsync(options);

            cart.PaymentIntentId = intent.Id;

            cart.ClientSecret = intent.ClientSecret;
        }
        else
        {
            var options = new PaymentIntentUpdateOptions
            {
                Amount = total
            };

            await service.UpdateAsync(cart.PaymentIntentId, options);
        }
    }

    private static async Task<long> ApplyDiscountAsync(AppCoupon appCoupon, long amount)
    {
        var couponService = new Stripe.CouponService();

        var coupon = await couponService.GetAsync(appCoupon.CouponId);

        if (coupon.AmountOff.HasValue)
        {
            amount -= (long)coupon.AmountOff * 100;
        }

        if (coupon.PercentOff.HasValue)
        {
            var discount = amount * (coupon.PercentOff.Value / 100);

            amount -= (long)discount;
        }

        return amount;
    }

    private async Task ValidateCartItemsInCartAsync(ShoppingCart cart)
    {
        foreach (var item in cart.Items)
        {
            var productItem = await _unitOfWork.Repository<Core.Entities.Product>().GetByIdAsync(item.ProductId)
                ?? throw new Exception("Problem getting product in cart.");

            if (item.Price != productItem.Price) item.Price = productItem.Price;
        }
    }

    private async Task<long?> GetShippingPriceAsync(ShoppingCart cart)
    {
        if (cart.DeliveryMethodId.HasValue)
        {
            var deliveryMethod = await _unitOfWork.Repository<DeliveryMethod>()
                .GetByIdAsync((int)cart.DeliveryMethodId)
                    ?? throw new Exception("Problem with delivery method.");

            return (long)deliveryMethod.Price * 100;
        }

        return null;
    }

    private static long CalculateSubtotal(ShoppingCart cart)
    {
        var itemTotal = cart.Items.Sum(x => x.Quantity * x.Price);

        return (long)itemTotal;
    }
}
