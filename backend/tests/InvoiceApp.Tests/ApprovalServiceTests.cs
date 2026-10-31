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
        Assert.Collection(result.Approvals,
            approval =>
            {
                Assert.Equal("demo.approver", approval.Username);
                Assert.Equal(ApprovalAuthority.Manager, approval.Authority);
            });
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

    [Theory]
    [InlineData("manager-one", ApprovalAuthority.Manager)]
    [InlineData("director-one", ApprovalAuthority.Director)]
    public void Approve_FirstHighValueApprovalRecordsNonFinalApproval(
        string username,
        ApprovalAuthority authority)
    {
        var invoice = InvoiceWith("INV-HIGH-VALUE", InvoiceStatus.PendingApproval, 500_000.01m);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        var result = service.Approve(invoice.Id, username, authority);

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
        var firstApproval = Assert.Single(result.Approvals);
        Assert.Equal(username, firstApproval.Username);
        Assert.Equal(authority, firstApproval.Authority);
        Assert.NotEqual(default, firstApproval.ApprovedAt);
        Assert.Null(result.DecidedBy);
        Assert.Null(result.DecidedAt);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Approve_DistinctDirectorCompletesHighValueInvoiceInApprovalOrder()
    {
        var invoice = InvoiceWith("INV-HIGH-VALUE", InvoiceStatus.PendingApproval, 500_000.01m);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);
        service.Approve(invoice.Id, "manager-one", ApprovalAuthority.Manager);
        var firstApproval = Assert.Single(invoice.Approvals);

        var result = service.Approve(invoice.Id, "director-two", ApprovalAuthority.Director);

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal(2, result.Approvals.Count);
        Assert.Equal(firstApproval, result.Approvals[0]);
        Assert.Equal("director-two", result.Approvals[1].Username);
        Assert.Equal(ApprovalAuthority.Director, result.Approvals[1].Authority);
        Assert.Equal("director-two", result.DecidedBy);
        Assert.Equal(result.Approvals[1].ApprovedAt, result.DecidedAt);
        Assert.Equal(2, repository.UpdateCount);
    }

    [Fact]
    public void Approve_RejectsSameUserAsSecondApproverWithoutChangingHighValueInvoice()
    {
        var invoice = InvoiceWith("INV-HIGH-VALUE", InvoiceStatus.PendingApproval, 500_000.01m);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);
        service.Approve(invoice.Id, "manager-one", ApprovalAuthority.Manager);
        var originalState = Capture(invoice);
        var originalUpdateCount = repository.UpdateCount;

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoice.Id, "manager-one", ApprovalAuthority.Manager));

        Assert.Contains("different user", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(originalState, Capture(invoice));
        Assert.Equal(originalUpdateCount, repository.UpdateCount);
    }

    [Fact]
    public void Approve_RejectsDifferentManagerAsSecondApproverWithoutChangingHighValueInvoice()
    {
        var invoice = InvoiceWith("INV-HIGH-VALUE", InvoiceStatus.PendingApproval, 500_000.01m);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);
        service.Approve(invoice.Id, "manager-one", ApprovalAuthority.Manager);
        var originalState = Capture(invoice);
        var originalUpdateCount = repository.UpdateCount;

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve(invoice.Id, "manager-two", ApprovalAuthority.Manager));

        Assert.Contains("Director", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(originalState, Capture(invoice));
        Assert.Equal(originalUpdateCount, repository.UpdateCount);
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
