namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through its lifecycle: Draft -> PendingApproval -> Approved or Rejected.
/// One invoice at a time, enforcing the authority limits for single-approver decisions.
/// </summary>
public class ApprovalService
{
    private readonly IInvoiceRepository _repository;

    public ApprovalService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public Invoice Submit(string invoiceId)
    {
        var invoice = Find(invoiceId);

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Invoice {invoiceId} is {invoice.Status}; only drafts can be submitted.");
        }

        invoice.Status = InvoiceStatus.PendingApproval;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Approve(string invoiceId, string approver, ApprovalAuthority authority)
    {
        var invoice = Find(invoiceId);
        EnsurePending(invoice);
        EnsureAuthorityCanApprove(invoice, authority);

        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = DateTime.UtcNow;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Reject(string invoiceId, string approver, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required to reject an invoice.", nameof(reason));
        }

        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        invoice.Status = InvoiceStatus.Rejected;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = DateTime.UtcNow;
        invoice.DecisionNote = reason;
        _repository.Update(invoice);
        return invoice;
    }

    private Invoice Find(string invoiceId) =>
        _repository.GetById(invoiceId)
        ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

    private static void EnsurePending(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided.");
        }
    }

    private static void EnsureAuthorityCanApprove(Invoice invoice, ApprovalAuthority authority)
    {
        if (authority is not ApprovalAuthority.Manager and not ApprovalAuthority.Director)
        {
            throw new InvalidOperationException("A valid Manager or Director approval authority is required.");
        }

        if (invoice.Amount > 500_000m)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is above 500,000 and requires the two-approver workflow; a single approval is not permitted.");
        }

        if (authority == ApprovalAuthority.Manager && invoice.Amount > 50_000m)
        {
            throw new InvalidOperationException(
                $"Manager authority is limited to invoices up to 50,000; invoice {invoice.Id} exceeds that limit.");
        }
    }
}
