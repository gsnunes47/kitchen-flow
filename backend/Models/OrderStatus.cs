using System.Text.Json.Serialization;

namespace PizzaFlow.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    EmPreparo,
    Cancelado,
    Finalizado
}
