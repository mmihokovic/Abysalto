namespace Abysalto.Cart.Api.Data
{
    public sealed class CartItem
    {
        public Guid CartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public Product Product { get; set; } = null!;
    }
}
