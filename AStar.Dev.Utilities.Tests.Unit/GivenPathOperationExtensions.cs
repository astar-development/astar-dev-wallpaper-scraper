namespace AStar.Dev.Utilities.Tests.Unit;

public sealed class GivenPathOperationExtensions
{
    [Fact]
    public void when_combine_path_is_called_with_relative_segments_then_returns_the_combined_path()
    {
        string basePath = Path.Join("root", "base");

        string result = basePath.CombinePath("child", "file.txt");

        result.ShouldBe("root/base/child/file.txt");
    }

    [Fact]
    public void when_combine_path_calls_are_chained_without_rooted_segments_then_returns_the_combined_path()
    {
        string result = "base".CombinePath("child2").CombinePath("file.txt");

        result.ShouldBe("base/child2/file.txt");
    }
}
