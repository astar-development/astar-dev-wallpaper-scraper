using AStar.Dev.Source.Generators.StrongIdCodeGeneration;
using AStar.Dev.Source.Generators.Tests.Unit.Utilitites;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AStar.Dev.Source.Generators.Tests.Unit.StrongIdCodeGeneration;

public class GivenStrongIdGenerator
{
    [Fact]
    public void when_int_id_type_is_specified_for_valid_readonly_record_struct_then_generates_partial_struct_with_int_id_property()
    {
        const string input = @"using AStar.Dev.Source.Generators.Attributes;
namespace TestNamespace
{
    [StrongId(typeof(int))]
    public readonly partial record struct MyId { }
}";

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        string generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("public readonly partial record struct MyId(System.Int32 Id)");
    }

    [Fact]
    public void when_string_id_type_is_specified_for_valid_readonly_record_struct_then_generates_partial_struct_with_string_id_property()
    {
        const string input = @"using AStar.Dev.Source.Generators.Attributes;
namespace TestNamespace
{
    [StrongId(typeof(string))]
    public readonly partial record struct MyId { }
}";

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        string generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("public readonly partial record struct MyId(System.String Id)");
    }

    [Fact]
    public void when_guid_id_type_is_specified_for_valid_readonly_record_struct_then_generates_partial_struct_with_guid_id_property()
    {
        const string input = @"using AStar.Dev.Source.Generators.Attributes;
namespace TestNamespace
{
    [StrongId(typeof(Guid))]
    public readonly partial record struct MyId { }
}";

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        string generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("public readonly partial record struct MyId(System.Guid Id)");
    }

    [Fact]
    public void when_no_id_type_is_specified_for_valid_readonly_record_struct_then_generates_partial_struct_with_default_guid_id_property()
    {
        const string input = @"using AStar.Dev.Source.Generators.Attributes;
namespace TestNamespace
{
    [StrongId]
    public readonly partial record struct MyId { }
}";

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeFalse();
        string generatedText = generated.SourceText.ToString();
        generatedText.ShouldContain("public readonly partial record struct MyId(System.Guid Id);");
    }

    [Fact]
    public void when_struct_is_not_readonly_or_not_a_record_then_does_not_generate()
    {
        const string input = @"using AStar.Dev.Source.Generators.Attributes;
namespace TestNamespace
{
    [StrongId(typeof(int))]
    public partial struct MyId { }
}";

        var compilation = CompilationHelpers.CreateCompilation(input);

        var generator = new StrongIdGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var result = driver.GetRunResult();
        var allGenerated = result.Results.SelectMany(r => r.GeneratedSources).ToList();
        var generated = allGenerated.FirstOrDefault(x => x.HintName.Contains("MyId", StringComparison.Ordinal));
        generated.Equals(default(GeneratedSourceResult)).ShouldBeTrue();
    }
}
