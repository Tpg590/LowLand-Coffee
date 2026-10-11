using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Order.API.Entities;

[Table("order_items")]
public class OrderItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Required]
    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [Column("unit_price", TypeName = "decimal(12,2)")]
    public decimal UnitPrice { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("options_summary")]
    public string? OptionsSummary { get; set; }

    [Column("subtotal", TypeName = "decimal(12,2)")]
    public decimal Subtotal { get; set; }

    // Navigation property
    [ForeignKey(nameof(OrderId))]
    public virtual Order Order { get; set; } = null!;
}
