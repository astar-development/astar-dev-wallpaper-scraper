namespace AStar.Dev.Source.Generators.Attributes.Tests.Unit;

public class GivenAutoRegisterOptionsAttribute
{
    [Fact]
    public void when_section_name_is_supplied_via_constructor_then_section_name_is_set()
        => new AutoRegisterOptionsAttribute("MySection").SectionName.ShouldBe("MySection");

    [Fact]
    public void when_section_name_is_null_then_section_name_property_is_null()
        => new AutoRegisterOptionsAttribute(null).SectionName.ShouldBeNull();

    [Fact]
    public void when_attribute_usage_is_inspected_then_valid_on_is_classes_and_structs()
        => typeof(AutoRegisterOptionsAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().ValidOn.ShouldBe(AttributeTargets.Class | AttributeTargets.Struct);

    [Fact]
    public void when_attribute_usage_is_inspected_then_inherited_is_false()
        => typeof(AutoRegisterOptionsAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().Inherited.ShouldBeFalse();

    [Fact]
    public void when_attribute_usage_is_inspected_then_allow_multiple_is_false()
        => typeof(AutoRegisterOptionsAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().AllowMultiple.ShouldBeFalse();
}
