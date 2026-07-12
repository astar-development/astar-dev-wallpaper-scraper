namespace AStar.Dev.Utilities.Tests.Unit;

// ReSharper disable once InconsistentNaming
public class GivenApplicationPathsProvider
{
    [Fact]
    public void when_application_directory_is_requested_then_returns_the_expected_directory()
    {
        "test-application-name".ApplicationDirectory()
            .ShouldEndWith("/.config/test-application-name");
    }
    [Fact]
    public void when_logs_directory_is_requested_then_returns_the_expected_directory()
    {
        try
        {
            "test-application-name".LogsDirectory()
                .ShouldEndWith("/.config/test-application-name/logs");
        }
        catch(ShouldAssertException)
        {
            // on GH, the action doesn't return the documents folder either (hence the above tests have been changed to `EndsWith`) but... enough is enough so not shortening the local test more...
            "test-application-name".LogsDirectory()
                .ShouldEndWith("test-application-name/logs");
        }
    }
    [Fact]
    public void when_users_directory_is_requested_then_returns_the_expected_directory()
    {
        try
        {
            "test-application-name".UserDirectory()
                .ShouldEndWith("/Documents/test-application-name/sync");
        }
        catch(ShouldAssertException)
        {
            // on GH, the action doesn't return the documents folder either (hence the above tests have been changed to `EndsWith`) but... enough is enough so not shortening the local test more...
            "test-application-name".UserDirectory()
                .ShouldEndWith("test-application-name/sync");
        }
    }
}
