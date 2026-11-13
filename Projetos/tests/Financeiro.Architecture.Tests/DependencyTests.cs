using System.Xml.Linq;

namespace Financeiro.Architecture.Tests;

public class DependencyTests
{
    [Fact]
    public void DomainAndApplicationDoNotDependOnInfrastructureOrHttp()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Financeiro.slnx"))) directory = directory.Parent;
        Assert.NotNull(directory);
        foreach (var layer in new[] { "Domain", "Application" })
        {
            var file = Path.Combine(directory.FullName, "src", $"Financeiro.{layer}", $"Financeiro.{layer}.csproj");
            var project = XDocument.Load(file);
            var references = project.Descendants("ProjectReference").Select(x => x.Attribute("Include")!.Value).ToArray();
            Assert.DoesNotContain(references, x => x.Contains("Infrastructure") || x.Contains("Api"));
            Assert.Empty(project.Descendants("FrameworkReference"));
            Assert.Empty(project.Descendants("PackageReference"));
        }
    }
}
