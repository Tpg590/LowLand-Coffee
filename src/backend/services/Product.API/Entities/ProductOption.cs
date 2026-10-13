using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Product.API.Entities;

[Table("product_options")]
public class ProductOption
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("option_type")]
    public string OptionType { get; set; } = "SIZE"; // SIZE, TOPPING, ICE_LEVEL, SUGAR_LEVEL

    [Required]
    [MaxLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("extra_price", TypeName = "decimal(12,2)")]
    public decimal ExtraPrice { get; set; } = 0.00m;

    [Column("is_available")]
    public bool IsAvailable { get; set; } = true;

    // Navigation property
    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;
}
