namespace AStar.Dev.Source.Generators.Attributes.Tests.Unit;

public class GivenAutoRegisterServiceAttribute
{
    [Fact]
    public void when_constructed_with_defaults_then_lifetime_is_scoped()
        => new AutoRegisterServiceAttribute().Lifetime.ShouldBe(ServiceLifetime.Scoped);

    [Fact]
    public void when_lifetime_is_supplied_via_constructor_then_lifetime_is_set()
        => new AutoRegisterServiceAttribute(ServiceLifetime.Singleton).Lifetime.ShouldBe(ServiceLifetime.Singleton);

    [Fact]
    public void when_as_property_is_set_then_as_returns_that_type()
        => new AutoRegisterServiceAttribute { As = typeof(string) }.As.ShouldBe(typeof(string));

    [Fact]
    public void when_as_self_property_is_set_then_as_self_is_true()
        => new AutoRegisterServiceAttribute { AsSelf = true }.AsSelf.ShouldBeTrue();

    [Fact]
    public void when_attribute_usage_is_inspected_then_valid_on_is_classes_only()
        => typeof(AutoRegisterServiceAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().ValidOn.ShouldBe(AttributeTargets.Class);

    [Fact]
    public void when_attribute_usage_is_inspected_then_inherited_is_false()
        => typeof(AutoRegisterServiceAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().Inherited.ShouldBeFalse();

    [Fact]
    public void when_attribute_usage_is_inspected_then_allow_multiple_is_false()
        => typeof(AutoRegisterServiceAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().AllowMultiple.ShouldBeFalse();
}
