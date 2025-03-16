using Core.Entities;

namespace Core.Interfaces;

public interface IPaymentService
{
    Task<ShoppingCart?> CreateOrUpdatePaymentIntentAsync(string cartId);
    Task<string> RefundPaymentAsync(string paymentIntentId);
}
