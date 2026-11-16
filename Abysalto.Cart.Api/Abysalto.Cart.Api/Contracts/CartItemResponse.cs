namespace Abysalto.Cart.Api.Contracts;

public sealed record CartItemResponse(
    int ProductId,
    string Name,
    int Quantity,
    decimal UnitPrice)
{
    public decimal LineTotal => Quantity * UnitPrice;
}