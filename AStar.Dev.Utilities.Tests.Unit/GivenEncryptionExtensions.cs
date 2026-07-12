namespace AStar.Dev.Utilities.Tests.Unit;

public class GivenEncryptionExtensions
{
    [Fact]
    public void when_a_value_is_encrypted_then_the_result_is_as_expected()
        => "irrelevant-string".Encrypt().ShouldBe("f4eR8dX1bWiwTF/GNd02dTl2wnPfj9jFHWi3ZVCxceQ=");

    [Fact]
    public void when_a_value_is_decrypted_then_the_result_is_as_expected()
        => "f4eR8dX1bWiwTF/GNd02dTl2wnPfj9jFHWi3ZVCxceQ=".Decrypt().ShouldBe("irrelevant-string");
}
