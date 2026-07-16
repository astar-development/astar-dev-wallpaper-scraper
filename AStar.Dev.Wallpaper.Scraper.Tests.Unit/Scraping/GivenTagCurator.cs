using AStar.Dev.Wallpaper.Scraper.Scraping;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenTagCurator
{
    [Fact]
    public void when_a_tag_matches_a_model_to_ignore_case_insensitively_then_it_is_excluded_and_a_message_explains_why()
    {
        List<TagData> tags = [new("Emma Stone", "people > actress")];

        var curation = TagCurator.Curate(tags, ["emma stone"], []);

        curation.Kept.ShouldBeEmpty();
        curation.Messages.ShouldContain(message => message.Contains("Ignoring model: Emma Stone"));
    }

    [Fact]
    public void when_a_tag_matches_a_tag_to_ignore_case_insensitively_then_it_is_excluded_and_a_message_explains_why()
    {
        List<TagData> tags = [new("Blue", "colors")];

        var curation = TagCurator.Curate(tags, [], ["BLUE"]);

        curation.Kept.ShouldBeEmpty();
        curation.Messages.ShouldContain(message => message.Contains("Ignoring tag: Blue"));
    }

    [Fact]
    public void when_a_tag_matches_neither_ignore_list_then_it_is_kept_and_every_step_is_reported()
    {
        List<TagData> tags = [new("Nature", "outdoors")];

        var curation = TagCurator.Curate(tags, ["some other model"], ["some other tag"]);

        curation.Kept.ShouldBe(tags);
        curation.Messages.ShouldContain(message => message.Contains("Found tag: Nature"));
        curation.Messages.ShouldContain(message => message.Contains("Model: Nature is not in the modelsToIgnore list"));
        curation.Messages.ShouldContain(message => message.Contains("Tag: Nature is not in the tagsToIgnore list"));
    }

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

    [Fact]
    public void when_the_stripped_tag_matches_a_model_to_ignore_then_it_is_excluded()
    {
        List<TagData> tags = [new("Emma Stone Model", "people > model")];

        var curation = TagCurator.Curate(tags, ["emma stone"], []);

        curation.Kept.ShouldBeEmpty();
        curation.Messages.ShouldContain(message => message.Contains("Ignoring model: Emma Stone"));
    }
}
