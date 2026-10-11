using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Order.API.Entities;

[Table("order_payments")]
public class OrderPayment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("payment_method")]
    public string PaymentMethod { get; set; } = Entities.PaymentMethod.CASH;

    [MaxLength(100)]
    [Column("transaction_code")]
    public string? TransactionCode { get; set; }

    [Column("amount", TypeName = "decimal(12,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("payment_status")]
    public string PaymentStatus { get; set; } = Entities.PaymentStatus.PENDING;

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(OrderId))]
    public virtual Order Order { get; set; } = null!;
}
