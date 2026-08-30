namespace PaymentService.Domain.Entities;

public class Payment
{
    public decimal Amount { get; private set; }

    public Payment(decimal amount)
    {
        Amount = amount;
    }

    public bool IsApproved()
    {
        // Regla de negocio: Aprobar pagos menores a 100000. Rechazar pagos mayores o iguales a 100000.
        return Amount < 100000m;
    }
}
