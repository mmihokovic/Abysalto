namespace Abysalto.Cart.Api.Data
{
    public sealed class Cart
    {
        public Guid Id { get; set; }
        public required string OwnerId { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public byte[] RowVersion { get; private set; } = [];
        public List<CartItem> Items { get; } = [];
    }
}
