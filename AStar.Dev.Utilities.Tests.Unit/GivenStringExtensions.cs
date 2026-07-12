namespace AStar.Dev.Utilities.Tests.Unit;

public sealed class GivenStringExtensions
{
    private const string AnyJson = "{\"AnyInt\":0,\"AnyString\":\"\"}";
    private const string NotNullString = "value does not matter";
    private const string WhitespaceString = " ";
#pragma warning disable CA1805
    private readonly string? nullString = null;
#pragma warning restore CA1805

    [Fact]
    public void when_is_null_is_called_then_returns_the_expected_result() =>
        nullString.IsNull().ShouldBeTrue();

    [Fact]
    public void when_is_not_null_is_called_then_returns_the_expected_result() =>
        NotNullString.IsNotNull().ShouldBeTrue();

    [Fact]
    public void when_is_null_or_white_space_is_called_then_returns_the_expected_result() =>
        WhitespaceString.IsNullOrWhiteSpace().ShouldBeTrue();

    [Fact]
    public void when_is_not_null_or_white_space_is_called_then_returns_the_expected_result() =>
        NotNullString.IsNotNullOrWhiteSpace().ShouldBeTrue();

    [Fact]
    public void when_from_json_is_called_then_returns_the_expected_result() =>
        AnyJson.FromJson<AnyClass>().ShouldBeEquivalentTo(new AnyClass());

    [Fact]
    public void when_from_json_is_called_with_json_serializer_options_then_returns_the_expected_result() =>
        AnyJson.FromJson<AnyClass>(new()).ShouldBeEquivalentTo(new AnyClass());

    [Theory]
    [InlineData("no-Extension", false)]
    [InlineData("Wrong-Extension.txt", false)]
    [InlineData("Wrong-Extension.DOC", false)]
    [InlineData("Wrong-Extension.PdF", false)]
    [InlineData("Correct-Extension.jpG", true)]
    [InlineData("Correct-Extension.jpeG", true)]
    [InlineData("Correct-Extension.bmp", true)]
    [InlineData("Write-Extension.png", true)]
    [InlineData("Correct-Extension.gif", true)]
    public void when_is_image_is_called_then_returns_the_expected_results(string fileName, bool expectedResponse) =>
        fileName.IsImage().ShouldBe(expectedResponse);

    [Theory]
    [InlineData("no-Truncation", 20, "no-Truncation")]
    [InlineData("Small-String-Truncation.txt", 10, "Small-Stri")]
    [InlineData("Small-String-Truncation.DOC", 15, "Small-String-Tr")]
    [InlineData("Small-String-Truncation.PdF", 20, "Small-String-Truncat")]
    [InlineData("Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String--Truncation.jpG", 100,
                "Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-Str")]
    [InlineData("Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String--Truncation.jpeG", 120,
                "Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Lar")]
    [InlineData("Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String--Truncation.bmp", 140,
                "Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-Stri")]
    [InlineData("Write-Truncation.png", 10, "Write-Trun")]
    [InlineData("Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String--Truncation.gif", 160,
                "Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String--Tru")]
    public void when_truncate_if_required_is_called_then_returns_the_expected_results(string fileName, int truncateLength, string expectedResponse) =>
        fileName.TruncateIfRequired(truncateLength).ShouldBe(expectedResponse);

    [Theory]
    [InlineData("no-number", false)]
    [InlineData("number-at-the-end-123", false)]
    [InlineData("123-number-at-the-beginning", false)]
    [InlineData("number-in-the-123-middle", false)]
    [InlineData("1", true)]
    [InlineData("12", true)]
    [InlineData("123456", true)]
    public void when_is_number_only_is_called_then_returns_the_expected_results(string fileName, bool expectedResponse) =>
        fileName.IsNumberOnly().ShouldBe(expectedResponse);


    [Theory]
    [InlineData("path/to-some_file.txt", "path to some file.txt")]
    [InlineData("folder/sub-folder/file_name.txt", "folder sub folder file name.txt")]
    [InlineData("already clean.txt", "already clean.txt")]
    [InlineData("-_-", "   ")]
    public void when_sanitize_file_path_is_called_then_returns_the_expected_result(string input, string expected) => input.SanitizeFilePath().ShouldBe(expected);

    [Theory]
    [InlineData(1, "1 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1024 * 1024, "1.0 MB")]
    [InlineData(1024 * 1024 * 1024, "1.0 GB")]
    [InlineData(0, "")]
    [InlineData(512, "512 B")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(5 * 1024 * 1024, "5.0 MB")]
    [InlineData((5 * 1024 * 1024) + (512 * 1024 * 1024), "517.0 MB")]
    public void when_file_size_to_text_is_called_then_returns_the_human_readable_format(long fileSizeToConvert, string expected) => fileSizeToConvert.FileSizeToText().ShouldBe(expected);

    [Theory]
    [InlineData("example text", "Example Text")]
    [InlineData("exampletext", "Exampletext")]
    [InlineData("EXAMPLE TEXT", "Example Text")]
    public void when_to_title_case_is_called_then_returns_the_expected_result(string input, string expected) => input.ToTitleCase().ShouldBe(expected);
}
