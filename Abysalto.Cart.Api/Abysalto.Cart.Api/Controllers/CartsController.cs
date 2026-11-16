using Abysalto.Cart.Api.Contracts;
using Abysalto.Cart.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CartEntity = Abysalto.Cart.Api.Data.Cart;

namespace Abysalto.Cart.Api.Controllers;

[ApiController]
[Authorize(Policy = "CartOwner")]
[Route("api/carts")]
public sealed class CartsController(CartDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CartResponse>> Create(CancellationToken cancellationToken)
    {
        var cart = new CartEntity
        {
            Id = Guid.NewGuid(),
            OwnerId = User.FindFirst("sub")!.Value,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        db.Carts.Add(cart);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = cart.Id },
            new CartResponse(cart.Id, cart.UpdatedAtUtc));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CartResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var cart = await db.Carts.AsNoTracking()
            .SingleOrDefaultAsync(cart => cart.Id == id, cancellationToken);

        var ownerId = User.FindFirst("sub")!.Value;
        if (cart is null || !string.Equals(cart.OwnerId, ownerId, StringComparison.Ordinal))
        {
            return NotFound();
        }

        return Ok(new CartResponse(cart.Id, cart.UpdatedAtUtc));
    }
}