using System;
using System.Collections.Generic;

namespace ECommerce.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public string? TransactionId { get; private set; }
        public string? PaymentRejectReason { get; private set; }

        public void MarkAsPaid(string transactionId)
        {
            Status = OrderStatus.Paid;
            TransactionId = transactionId;
        }

        public void MarkPaymentAsRejected(string reason)
        {
            Status = OrderStatus.PaymentRejected;
            PaymentRejectReason = reason;
        }

        public void Cancel()
        {
            Status = OrderStatus.Cancelled;
        }
    }
}
