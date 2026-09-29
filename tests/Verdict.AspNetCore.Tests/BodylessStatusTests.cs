using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Verdict.AspNetCore.Tests;

/// <summary>
/// A success mapped to 204 or 304 used to write the value as JSON, and Kestrel
/// throws when a response with one of those codes gets a body.
/// </summary>
public class BodylessStatusTests
{
    [Theory]
    [InlineData(204)]
    [InlineData(304)]
    public void ToHttpResultSendsNoBody(int status)
    {
        var result = Result<int>.Success(42).ToHttpResult(successStatusCode: status);

        var code = Assert.IsType<StatusCodeHttpResult>(result);
        Assert.Equal(status, code.StatusCode);
    }

    [Theory]
    [InlineData(204)]
    [InlineData(304)]
    public void ToHttpResultWithAContextSendsNoBody(int status)
    {
        var result = Result<int>.Success(42).ToHttpResult(new DefaultHttpContext(), status);

        var code = Assert.IsType<StatusCodeHttpResult>(result);
        Assert.Equal(status, code.StatusCode);
    }

    [Fact]
    public void ToActionResultWithAContextSendsNoBody()
    {
        var result = Result<int>.Success(42).ToActionResult(new DefaultHttpContext(), 204);

        var code = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(204, code.StatusCode);
    }

    [Fact]
    public void ALocationDoesNotBringTheBodyBack()
    {
        var result = Result<int>.Success(42).ToActionResult(successStatusCode: 204, locationUri: "/items/1");

        Assert.IsType<NoContentResult>(result.Result);
    }

    [Fact]
    public void A304IsNotWrittenAsAnObject()
    {
        var result = Result<int>.Success(42).ToActionResult(successStatusCode: 304);

        var code = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(304, code.StatusCode);
    }

    [Fact]
    public void A200StillCarriesTheValue()
    {
        var result = Result<int>.Success(42).ToHttpResult();

        var json = Assert.IsType<JsonHttpResult<int>>(result);
        Assert.Equal(42, json.Value);
    }
}
