using Abysalto.Cart.Api.Contracts;
using Abysalto.Cart.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using CartEntity = Abysalto.Cart.Api.Data.Cart;

namespace Abysalto.Cart.Api.Controllers;

[ApiController]
[Authorize(Policy = "CartOwner")]
[Route("api/carts")]
public sealed class CartsController(CartDbContext db) : ControllerBase
{
    private const int DuplicateUniqueIndexError = 2601;
    private const int DuplicateKeyConstraintError = 2627;

    [HttpPost]
    public async Task<ActionResult<CartResponse>> Create(
        CancellationToken cancellationToken)
    {
        var cart = new CartEntity
        {
            Id = Guid.NewGuid(),
            OwnerId = User.FindFirst("sub")!.Value,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        db.Carts.Add(cart);
        await db.SaveChangesAsync(cancellationToken);

        Response.Headers[HeaderNames.ETag] = GetCartETag(cart);

        return CreatedAtAction(
            nameof(GetById),
            new { id = cart.Id },
            new CartResponse(cart.Id, cart.UpdatedAtUtc, []));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CartResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var cart = await db.Carts.AsNoTracking()
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (cart is null || !IsOwner(cart))
        {
            return NotFound();
        }

        var items = cart.Items
            .OrderBy(x => x.ProductId)
            .Select(x => new CartItemResponse(
                x.ProductId,
                x.Product.Name,
                x.Quantity,
                x.Product.UnitPrice))
            .ToArray();

        Response.Headers[HeaderNames.ETag] = GetCartETag(cart);

        return Ok(new CartResponse(cart.Id, cart.UpdatedAtUtc, items));
    }

    [HttpPut("{id:guid}/items/{productId:int}")]
    public async Task<IActionResult> SetItemQuantity(
        Guid id,
        int productId,
        SetCartItemQuantityRequest request,
        CancellationToken cancellationToken)
    {
        var cart = await db.Carts.FindAsync([id], cancellationToken);
        if (cart is null || !IsOwner(cart))
        {
            return NotFound();
        }

        var item = await db.CartItems.FindAsync([id, productId], cancellationToken);
        if (item is null &&
            !await db.Products.AnyAsync(x => x.Id == productId, cancellationToken))
        {
            return NotFound();
        }

        if (CheckIfMatch(cart) is { } failure)
        {
            return failure;
        }

        if (item is null)
        {
            db.CartItems.Add(new CartItem
            {
                CartId = id,
                ProductId = productId,
                Quantity = request.Quantity
            });
        }
        else if (item.Quantity == request.Quantity)
        {
            return NoContent();
        }
        else
        {
            item.Quantity = request.Quantity;
        }

        return await SaveCartChangeAsync(cart, cancellationToken);
    }

    [HttpDelete("{id:guid}/items/{productId:int}")]
    public async Task<IActionResult> RemoveItem(
        Guid id,
        int productId,
        CancellationToken cancellationToken)
    {
        var cart = await db.Carts.FindAsync([id], cancellationToken);
        if (cart is null || !IsOwner(cart))
        {
            return NotFound();
        }

        if (CheckIfMatch(cart) is { } failure)
        {
            return failure;
        }

        var item = await db.CartItems.FindAsync([id, productId], cancellationToken);
        if (item is null)
        {
            return NoContent();
        }

        db.CartItems.Remove(item);
        return await SaveCartChangeAsync(cart, cancellationToken);
    }

    private bool IsOwner(CartEntity cart) =>
        string.Equals(cart.OwnerId, User.FindFirst("sub")!.Value, StringComparison.Ordinal);

    private async Task<IActionResult> SaveCartChangeAsync(
        CartEntity cart,
        CancellationToken cancellationToken)
    {
        cart.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.Entry(cart).Property(x => x.UpdatedAtUtc).IsModified = true;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        catch (DbUpdateException exception) when (
            exception is DbUpdateConcurrencyException ||
            (exception.InnerException is SqlException sqlException &&
             (sqlException.Number == DuplicateUniqueIndexError ||
              sqlException.Number == DuplicateKeyConstraintError)))
        {
            return CartVersionChanged();
        }
    }

    private static string GetCartETag(CartEntity cart) =>
        $"\"{Convert.ToBase64String(cart.RowVersion)}\"";

    private ObjectResult? CheckIfMatch(CartEntity cart)
    {
        if (!Request.Headers.TryGetValue(HeaderNames.IfMatch, out var values))
        {
            return Problem(
                statusCode: StatusCodes.Status428PreconditionRequired,
                detail: "Load the cart and send its ETag in the If-Match header.");
        }

        if (values.Count != 1 ||
            !EntityTagHeaderValue.TryParse(values[0], out var suppliedTag) ||
            suppliedTag.Tag.Value == "*")
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: "If-Match must contain one explicit cart ETag.");
        }

        if (suppliedTag.IsWeak ||
            !string.Equals(suppliedTag.Tag.Value, GetCartETag(cart), StringComparison.Ordinal))
        {
            return CartVersionChanged();
        }

        return null;
    }

    private ObjectResult CartVersionChanged() =>
        Problem(
            statusCode: StatusCodes.Status412PreconditionFailed,
            detail: "The cart has changed. Reload it before trying again.");
}