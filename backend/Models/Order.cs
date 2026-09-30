using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace KitchenFlow.Models;

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string CustomerName { get; set; }
    public required string Phone { get; set; }
    public required JsonDocument Items { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.EmPreparo;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [NotMapped]
    public int TotalPrice
    {
        get
        {
            var items = Items.Deserialize<List<OrderItem>>(
                JsonSerializerOptions.Web
            ) ?? [];

            return items.Sum(item => item.Quantity * item.Price);
        }
    }
}
