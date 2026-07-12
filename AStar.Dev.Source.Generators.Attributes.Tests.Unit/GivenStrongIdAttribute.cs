namespace AStar.Dev.Source.Generators.Attributes.Tests.Unit;

public class GivenStrongIdAttribute
{
    [Fact]
    public void when_no_type_is_specified_then_id_type_defaults_to_guid()
        => new StrongIdAttribute().IdType.ShouldBe(typeof(Guid));

    [Fact]
    public void when_id_type_is_supplied_via_constructor_then_id_type_is_set()
        => new StrongIdAttribute(typeof(int)).IdType.ShouldBe(typeof(int));

    [Fact]
    public void when_attribute_usage_is_inspected_then_valid_on_is_structs_only()
        => typeof(StrongIdAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().ValidOn.ShouldBe(AttributeTargets.Struct);

    [Fact]
    public void when_attribute_usage_is_inspected_then_inherited_is_false()
        => typeof(StrongIdAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().Inherited.ShouldBeFalse();

    [Fact]
    public void when_attribute_usage_is_inspected_then_allow_multiple_is_false()
        => typeof(StrongIdAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().AllowMultiple.ShouldBeFalse();
}
