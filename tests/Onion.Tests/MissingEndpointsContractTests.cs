using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Onion.Controllers;
using Xunit;

namespace Onion.Tests;

public sealed class MissingEndpointsContractTests
{
    [Fact]
    public void ManageRequest_ExposesCollectionGet()
    {
        var method = typeof(ManageRequestController).GetMethod(nameof(ManageRequestController.GetAll));

        Assert.NotNull(method);
        Assert.Contains(method!.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true)
            .Cast<HttpGetAttribute>(), attribute => attribute.Template is null);
    }

    [Fact]
    public void Approvals_ExposeCanonicalCollectionAndReviewActions()
    {
        var methods = typeof(DeletionApprovalController).GetMethods();

        Assert.Contains(methods, method => method.Name == nameof(DeletionApprovalController.GetAll));
        Assert.Contains(methods, method => method.Name == nameof(DeletionApprovalController.Approve));
        Assert.Contains(methods, method => method.Name == nameof(DeletionApprovalController.Reject));
    }

    [Fact]
    public void DgiiReports_UsesGlobalVersionedRoute()
    {
        var route = typeof(DgiiReportsController).GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .Cast<RouteAttribute>()
            .Single();

        Assert.Equal("dgii", route.Template);
        Assert.NotNull(typeof(DgiiReportsController).GetMethod(nameof(DgiiReportsController.Get606)));
        Assert.NotNull(typeof(DgiiReportsController).GetMethod(nameof(DgiiReportsController.Get607)));
    }
}
