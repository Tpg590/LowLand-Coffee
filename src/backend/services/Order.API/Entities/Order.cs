using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Order.API.Entities;

[Table("order_orders")]
public class Order
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("order_code")]
    public string OrderCode { get; set; } = string.Empty;

    [Column("total_amount", TypeName = "decimal(12,2)")]
    public decimal TotalAmount { get; set; }

    [Column("discount_amount", TypeName = "decimal(12,2)")]
    public decimal DiscountAmount { get; set; } = 0.00m;

    [Column("final_amount", TypeName = "decimal(12,2)")]
    public decimal FinalAmount { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = OrderStatus.PENDING;

    [Column("cancel_reason")]
    public string? CancelReason { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<OrderPayment> OrderPayments { get; set; } = new List<OrderPayment>();
}
