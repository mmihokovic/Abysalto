using System;
using Abysalto.Cart.Api.Contracts;
using Xunit;

namespace Abysalto.Cart.Tests;

public sealed class CartResponseTests
{
    [Fact]
    public void Total_MultipliesEachUnitPriceByItsQuantity()
    {
        var cart = new CartResponse(Guid.Empty, DateTimeOffset.UnixEpoch,
        [
            new CartItemResponse(1, "Product 1", 2, 9.99m),
            new CartItemResponse(2, "Product 2", 3, 2.01m)
        ]);

        Assert.Equal(26.01m, cart.Total);
    }

    [Fact]
    public void Total_IsZeroForAnEmptyCart()
    {
        var cart = new CartResponse(Guid.Empty, DateTimeOffset.UnixEpoch, []);

        Assert.Equal(0m, cart.Total);
    }
}
