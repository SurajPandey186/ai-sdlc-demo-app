using System.Reflection;
using InvoiceApp.Api.Auth;
using InvoiceApp.Api.Controllers;
using InvoiceApp.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace InvoiceApp.Tests;

public class AuthControllerTests
{
    [Theory]
    [InlineData("demo.approver", "Approver", ApprovalAuthority.Manager)]
    [InlineData("demo.admin", "Admin", ApprovalAuthority.Director)]
    public void Login_CreatesSessionWithConfiguredAuthorityAndOriginalAccessRole(
        string username,
        string expectedRole,
        ApprovalAuthority expectedAuthority)
    {
        var options = LoadDemoUsers();
        var user = Assert.Single(options.Users, candidate => candidate.Username == username);
        var sessions = new SessionStore();
        var controller = new AuthController(Options.Create(options), sessions);

        var result = controller.Login(new AuthController.LoginRequest(user.Username, user.Password));

        var response = Assert.IsType<AuthController.LoginResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        var session = Assert.IsType<UserSession>(sessions.Get(response.Token));
        Assert.Equal(expectedRole, session.Role);
        Assert.Equal(expectedAuthority, session.ApprovalAuthority);
    }

    [Theory]
    [InlineData("Approver", ApprovalAuthority.Manager, true)]
    [InlineData("Admin", ApprovalAuthority.Director, true)]
    [InlineData("Manager", ApprovalAuthority.Director, false)]
    [InlineData("Director", ApprovalAuthority.Manager, false)]
    public void ApprovalsController_RetainsExistingEndpointAccessRoles(
        string accessRole,
        ApprovalAuthority authority,
        bool expectedAllowed)
    {
        var controller = new ApprovalsController(new ApprovalService(new FakeInvoiceRepository()));
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextExtensions.SessionKey] = new UserSession("test-user", accessRole, authority);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var filterContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller);
        var roleFilter = typeof(ApprovalsController).GetCustomAttribute<RequireRoleAttribute>();

        Assert.NotNull(roleFilter);
        roleFilter.OnActionExecuting(filterContext);

        if (expectedAllowed)
        {
            Assert.Null(filterContext.Result);
        }
        else
        {
            Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<StatusCodeResult>(filterContext.Result).StatusCode);
        }
    }

    private static DemoUsersOptions LoadDemoUsers()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("approval-test-appsettings.json", optional: false)
            .Build();

        return new DemoUsersOptions
        {
            Users = configuration.GetSection("Users").Get<List<DemoUser>>()
                ?? throw new InvalidOperationException("Demo user configuration could not be loaded for the test.")
        };
    }
}
