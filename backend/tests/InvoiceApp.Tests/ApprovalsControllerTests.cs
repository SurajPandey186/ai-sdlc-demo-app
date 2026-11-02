using InvoiceApp.Api.Auth;
using InvoiceApp.Api.Controllers;
using InvoiceApp.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalsControllerTests
{
    [Fact]
    public void Approve_PassesAuthenticatedAuthorityToDomainService()
    {
        var invoice = new Invoice
        {
            Id = "INV-MANAGER-OVER-LIMIT",
            CustomerName = "Test School",
            Amount = 50_000.01m,
            Status = InvoiceStatus.PendingApproval
        };
        var repository = new FakeInvoiceRepository(invoice);
        var controller = new ApprovalsController(new ApprovalService(repository));
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextExtensions.SessionKey] = new UserSession(
            "demo.approver",
            "Approver",
            ApprovalAuthority.Manager);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = controller.Approve(invoice.Id);

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Equal(0, repository.UpdateCount);
    }
}
