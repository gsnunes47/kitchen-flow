using System.Text.Json.Serialization;

namespace KitchenFlow.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    EmPreparo,
    Cancelado,
    Finalizado
}
