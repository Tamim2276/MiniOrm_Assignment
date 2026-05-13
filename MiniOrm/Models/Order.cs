using System;
using MiniOrm.Attributes;

namespace MiniOrm.Models;

[Table("orders")]
public class Order
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column("order_date")]
    public DateTime OrderDate { get; set; }

    [Column("customer_name")]
    public string CustomerName { get; set; } = string.Empty;

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }
}
