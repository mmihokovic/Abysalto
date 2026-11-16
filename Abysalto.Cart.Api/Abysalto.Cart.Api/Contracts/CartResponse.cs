namespace Abysalto.Cart.Api.Contracts;

public sealed record CartResponse(
    Guid Id,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<CartItemResponse> Items)
{
    public string Currency => "EUR";
    public decimal Total => Items.Sum(item => item.LineTotal);
}