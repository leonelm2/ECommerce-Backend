using System; // Importa DateTime
using System.Collections.Generic; // Importa ICollection y List

namespace ECommerce.Domain.Entities // Capa Domain: el corazón del negocio, no depende de ninguna otra capa
{
    // ENTIDAD DE DOMINIO "Order": Representa una orden de compra en nuestro negocio y la tabla "Orders" en la base de datos.
    // Aplica DOMINIO RICO (Rich Domain Model) porque encapsula el cambio de estado mediante métodos de negocio.
    public class Order
    {
        public int Id { get; set; } // Clave primaria (Primary Key) autogenerada por la base de datos
        public int UserId { get; set; } // Clave foránea (Foreign Key) hacia el usuario que realizó la compra
        public User User { get; set; } = null!; // Propiedad de navegación de EF Core hacia la entidad User
        public DateTime OrderDate { get; set; } // Fecha y hora UTC en que se creó la orden
        public decimal TotalAmount { get; set; } // Monto total de la orden (suma de Quantity * UnitPrice de sus ítems)
        public OrderStatus Status { get; set; } = OrderStatus.Pending; // Estado actual de la orden (arranca en Pending por defecto)
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>(); // Relación 1 a N: lista de renglones/ítems de esta orden

        // ¡ENCAPSULAMIENTO DE DOMINIO RICO! Tienen "private set;" para que nadie fuera de esta clase pueda modificarlos libremente.
        public string? TransactionId { get; private set; } // Código de transacción devuelto por PaymentService cuando aprueba el pago
        public string? PaymentRejectReason { get; private set; } // Motivo del rechazo si PaymentService rechaza el pago o da timeout

        // Método de negocio para aprobar la orden: cambia el estado a Paid y registra el código de transacción
        public void MarkAsPaid(string transactionId)
        {
            Status = OrderStatus.Paid; // Pasa el estado a Pagado
            TransactionId = transactionId; // Guarda el comprobante de la transacción
        }

        // Método de negocio para rechazar el pago: cambia el estado a PaymentRejected y guarda la razón del fallo
        public void MarkPaymentAsRejected(string reason)
        {
            Status = OrderStatus.PaymentRejected; // Pasa el estado a Pago Rechazado
            PaymentRejectReason = reason; // Guarda el motivo (ej. "El monto supera el límite permitido" o "Timeout")
        }

        // Método de negocio para cancelar la orden ante un fallo interno inesperado
        public void Cancel()
        {
            Status = OrderStatus.Cancelled; // Pasa el estado a Cancelado
        }
    }
}
