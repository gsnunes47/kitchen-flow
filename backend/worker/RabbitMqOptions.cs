using System.ComponentModel.DataAnnotations;

namespace KitchenFlow.Worker;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public string HostName { get; init; } = string.Empty;

    [Required]
    public string QueueName { get; init; } = string.Empty;
}
