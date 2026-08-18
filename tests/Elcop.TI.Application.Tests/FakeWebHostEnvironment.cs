using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Elcop.TI.Application.Tests;

public sealed class FakeWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = "";
    public IFileProvider WebRootFileProvider { get; set; } = null!;
    public string ApplicationName { get; set; } = "Elcop.TI.Application.Tests";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
    public string ContentRootPath { get; set; } = "";
    public string EnvironmentName { get; set; } = "Testing";
}
