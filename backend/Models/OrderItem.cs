namespace PizzaFlow.Models;

public sealed record OrderItem(
    string Name,
    int Quantity,
    int Price //centavos
);
