namespace PaymentService.Domain.Entities; // Capa Domain del microservicio PaymentService

// ENTIDAD DE DOMINIO "Payment": Aplica Dominio Rico encapsulando la regla de aprobación del pago
public class Payment
{
    // Propiedad con "private set;" para que el monto no pueda ser alterado una vez creado el objeto Payment
    public decimal Amount { get; private set; }

    // Constructor: recibe el monto a cobrar al instanciar la entidad
    public Payment(decimal amount)
    {
        Amount = amount;
    }

    // REGLA DE NEGOCIO EN EL DOMINIO:
    // El método vive dentro de la entidad Payment (y no suelto en el Handler o Controller).
    public bool IsApproved()
    {
        // Regla de negocio: Aprobar pagos menores a $100.000. Rechazar pagos mayores o iguales a $100.000.
        return Amount < 100000m;
    }
}
