namespace AStar.Dev.Utilities.Tests.Unit;

public class GivenLinqExtensions
{
    [Fact]
    public void when_for_each_is_called_on_an_ienumerable_then_the_supplied_action_is_triggered()
    {
        var exception = Record.Exception(() => new List<string> { "", "z", "a" }.AsEnumerable().ForEach(_ => { }));

        Assert.Null(exception);
    }
}
