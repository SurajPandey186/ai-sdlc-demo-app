using System.Text.Json;
using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private static Invoice InvoiceWith(string id, InvoiceStatus status, decimal amount = 100m) =>
        new() { Id = id, CustomerName = "Test School", Amount = amount, Status = status };

    private static ApprovalService ServiceFor(params Invoice[] invoices) =>
        new(new FakeInvoiceRepository(invoices));

    [Fact]
    public void Submit_MovesDraftToPendingApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);

        var result = ServiceFor(invoice).Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
    }

    [Fact]
    public void Submit_RejectsInvoiceThatIsNotADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));
    }

    [Fact]
    public void Approve_SetsStatusApproverAndTimestamp()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Approve("INV-1", "demo.approver", ApprovalAuthority.Manager);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.approver", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsStillADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Draft));

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", "demo.approver", ApprovalAuthority.Manager));
    }

    [Fact]
    public void Approve_ThrowsWhenInvoiceDoesNotExist()
    {
        var service = ServiceFor();

        Assert.Throws<KeyNotFoundException>(() => service.Approve("INV-404", "demo.approver", ApprovalAuthority.Manager));
    }

    [Fact]
    public void Approve_AllowsManagerAtExactLimit()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 50_000m));

        var result = service.Approve("INV-1", "manager", ApprovalAuthority.Manager);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("manager", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_AllowsDirectorAtExactLimit()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_000m));

        var result = service.Approve("INV-1", "director", ApprovalAuthority.Director);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("director", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_AllowsDirectorBelowLimit()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 50_000.01m));

        var result = service.Approve("INV-1", "director", ApprovalAuthority.Director);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
    }

    [Theory]
    [InlineData("50000.01", ApprovalAuthority.Manager, "50,000")]
    [InlineData("500000", ApprovalAuthority.Manager, "50,000")]
    [InlineData("500000.01", ApprovalAuthority.Manager, "two-approver")]
    [InlineData("500000.01", ApprovalAuthority.Director, "two-approver")]
    public void Approve_RejectsOverLimitWithoutChangingInvoiceOrRepository(
        string amountText,
        ApprovalAuthority authority,
        string expectedReason)
    {
        var amount = decimal.Parse(amountText, System.Globalization.CultureInfo.InvariantCulture);
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, amount);
        var originalState = Capture(invoice);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "approver", authority));

        Assert.Contains(expectedReason, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(originalState, Capture(invoice));
        Assert.Equal(0, repository.UpdateCount);
    }

    private static string Capture(Invoice invoice) => JsonSerializer.Serialize(invoice);

    [Fact]
    public void Reject_RecordsReasonAndApprover()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Reject("INV-1", "demo.approver", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("Duplicate invoice", result.DecisionNote);
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        Assert.Throws<ArgumentException>(() => service.Reject("INV-1", "demo.approver", "  "));
    }
}
