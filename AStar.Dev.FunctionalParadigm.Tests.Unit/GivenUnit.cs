namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenUnit
{
    [Fact]
    public void when_unit_values_are_compared_then_they_are_all_equal()
    {
        var a = global::AStar.Dev.FunctionalParadigm.Unit.Value;
        var b = new FunctionalParadigm.Unit();

        a.ShouldBe(b);
        global::AStar.Dev.FunctionalParadigm.Unit.Value.ShouldBe(a);
    }
}
