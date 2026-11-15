namespace Abysalto.Cart.Api.Data
{
    public sealed class Product
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
