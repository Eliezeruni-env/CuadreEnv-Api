using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Onion.Controllers;
using Xunit;

namespace Onion.Tests;

public sealed class DgiiReportsControllerTests
{
    [Theory]
    [InlineData(nameof(DgiiReportsController.Get607))]
    [InlineData(nameof(DgiiReportsController.Download607))]
    [InlineData(nameof(DgiiReportsController.Download607Canonical))]
    [InlineData(nameof(DgiiReportsController.Get606))]
    [InlineData(nameof(DgiiReportsController.Download606))]
    [InlineData(nameof(DgiiReportsController.Download606Canonical))]
    [InlineData(nameof(DgiiReportsController.Get608))]
    [InlineData(nameof(DgiiReportsController.Download608))]
    [InlineData(nameof(DgiiReportsController.Download608Canonical))]
    public void RequiresValidYearAndMonth(string methodName)
    {
        var method = typeof(DgiiReportsController).GetMethod(methodName);

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        var year = parameters.Single(parameter => parameter.Name == "year");
        var month = parameters.Single(parameter => parameter.Name == "month");

        Assert.Equal(2000, year.GetCustomAttribute<RangeAttribute>()!.Minimum);
        Assert.Equal(2100, year.GetCustomAttribute<RangeAttribute>()!.Maximum);
        Assert.Equal(1, month.GetCustomAttribute<RangeAttribute>()!.Minimum);
        Assert.Equal(12, month.GetCustomAttribute<RangeAttribute>()!.Maximum);
    }
}
