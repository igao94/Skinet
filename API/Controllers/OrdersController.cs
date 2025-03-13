using API.DTOs;
using API.Extensions;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class OrdersController(ICartService cartService,
    IUnitOfWork unitOfWork) : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(CreateOrderDto createOrderDto)
    {
        var email = User.GetEmail();

        var cart = await cartService.GetCartAsync(createOrderDto.CartId);

        if (cart is null) return BadRequest("Cart not found.");

        if (cart.PaymentIntentId is null) return BadRequest("No payment intent for this order.");

        List<OrderItem> items = [];

        foreach (var item in cart.Items)
        {
            var productItem = await unitOfWork.Repository<Product>().GetByIdAsync(item.ProductId);

            if (productItem is null) return BadRequest("Problem with the order.");

            var itemOrdered = new ProductItemOrdered
            {
                ProductId = item.ProductId,
                PictureUrl = item.PictureUrl,
                ProductName = item.ProductName
            };

            var orderItem = new OrderItem
            {
                ItemOrdered = itemOrdered,
                Price = productItem.Price,
                Quantity = item.Quantity
            };

            items.Add(orderItem);
        }

        var deliveryMethod = await unitOfWork.Repository<DeliveryMethod>()
            .GetByIdAsync(createOrderDto.DeliveryMethodId);

        if (deliveryMethod is null) return BadRequest("No delivery method selected.");

        var order = new Order
        {
            OrderItems = items,
            DeliveryMethod = deliveryMethod,
            ShippingAddress = createOrderDto.ShippingAddress,
            Subtotal = items.Sum(x => x.Price * x.Quantity),
            PaymentSummary = createOrderDto.PaymentSummary,
            PaymentIntentId = cart.PaymentIntentId,
            BuyerEmail = email
        };

        unitOfWork.Repository<Order>().Add(order);

        var result = await unitOfWork.CompleteAsync();

        return result ? order : BadRequest("Problem creating order.");
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrdersForUser()
    {
        var spec = new OrderSpecification(User.GetEmail());

        var orders = await unitOfWork.Repository<Order>().ListAllWithSpecAsync(spec);

        var ordersToReturn = orders.Select(o => o.ToDto()).ToList();

        return Ok(ordersToReturn);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(int id)
    {
        var spec = new OrderSpecification(User.GetEmail(), id);

        var order = await unitOfWork.Repository<Order>().GetEntityWithSpecAsync(spec);

        if (order is null) return NotFound();

        return order.ToDto();
    }
}
