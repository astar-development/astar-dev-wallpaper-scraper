using AStar.Dev.Wallpaper.Scraper.Scraping;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenTagCurator
{
    [Fact]
    public void when_multiple_tags_are_curated_then_kept_tags_preserve_their_original_relative_order()
    {
        List<TagData> tags = [new("Nature", null), new("Ignored", null), new("City", null)];

        var curation = TagCurator.Curate(tags, ["ignored"], []);

        curation.Kept.ShouldBe([tags[0], tags[2]]);
    }

    [Fact]
    public void when_a_tag_is_exactly_model_then_it_is_dropped_entirely_and_not_reported()
    {
        List<TagData> tags = [new("model", "people > model")];

        var curation = TagCurator.Curate(tags, [], []);

        curation.Kept.ShouldBeEmpty();
        curation.Messages.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_tag_ends_with_model_then_the_suffix_is_stripped_before_it_is_kept()
    {
        List<TagData> tags = [new("Emma Stone Model", "people > model")];

        var curation = TagCurator.Curate(tags, [], []);

        curation.Kept.ShouldBe([new TagData("Emma Stone", "people > model")]);
        curation.Messages.ShouldContain(message => message.Contains("Found tag: Emma Stone,"));
    }

    [Fact]
    public void when_the_word_model_appears_mid_tag_then_only_the_trailing_suffix_is_stripped()
    {
        List<TagData> tags = [new("Super Model Model", "people > model")];

        var curation = TagCurator.Curate(tags, [], []);

        curation.Kept.ShouldBe([new TagData("Super Model", "people > model")]);
    }
}
