using System.ComponentModel.DataAnnotations;

namespace Abysalto.Cart.Api.Contracts;

public sealed class SetCartItemQuantityRequest
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }
}