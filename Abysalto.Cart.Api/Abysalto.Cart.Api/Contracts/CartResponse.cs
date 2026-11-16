namespace Abysalto.Cart.Api.Contracts;

public sealed record CartResponse(Guid Id, DateTimeOffset UpdatedAtUtc);