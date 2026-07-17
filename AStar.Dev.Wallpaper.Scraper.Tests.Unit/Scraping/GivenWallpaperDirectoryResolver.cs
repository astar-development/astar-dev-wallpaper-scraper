using AStar.Dev.Wallpaper.Scraper.Scraping;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenWallpaperDirectoryResolver
{
    private static readonly DirectoryLayout _layout = new("/root", "/regular", "/famous");
    private static readonly ScrapeCategory _category = new("Landscapes", "https://example.com/landscapes");

    [Fact]
    public void when_no_tags_are_famous_then_the_regular_base_directory_is_used()
    {
        var directory = WallpaperDirectoryResolver.Resolve(_layout, [new TagData("Nature", "outdoors")], _category);

        directory.ShouldBe(Path.Combine("/root/regular", "L", _category.Name, "Nature"));
    }

    [Fact]
    public void when_any_tag_is_famous_then_the_famous_base_directory_is_used_instead()
    {
        List<TagData> tags = [new("Emma Stone", "people > actress")];

        var directory = WallpaperDirectoryResolver.Resolve(_layout, tags, _category);

        directory.ShouldStartWith(Path.Combine("/root/famous/L", _category.Name));
    }

    [Fact]
    public void when_there_are_no_tags_then_only_the_root_base_and_category_directories_are_returned()
    {
        var directory = WallpaperDirectoryResolver.Resolve(_layout, [], _category);

        directory.ShouldBe(Path.Combine("/root/regular/L", _category.Name));
    }

    [Fact]
    public void when_a_tag_has_no_category_then_it_contributes_no_path_segment()
    {
        var directory = WallpaperDirectoryResolver.Resolve(_layout, [new TagData("Untagged", null)], _category);

        directory.ShouldBe(Path.Combine("/root/regular/L", _category.Name));
    }

    [Fact]
    public void when_a_tag_matches_the_category_name_then_it_contributes_no_path_segment()
    {
        var directory = WallpaperDirectoryResolver.Resolve(_layout, [new TagData(_category.Name, "outdoors")], _category);

        directory.ShouldBe(Path.Combine("/root/regular/L", _category.Name));
    }

    [Fact]
    public void when_a_tag_matches_the_category_name_with_different_casing_then_it_contributes_no_path_segment()
    {
        var directory = WallpaperDirectoryResolver.Resolve(_layout, [new TagData(_category.Name.ToUpperInvariant(), "outdoors")], _category);

        directory.ShouldBe(Path.Combine("/root/regular/L", _category.Name));
    }

    [Fact]
    public void when_tags_are_a_mix_of_famous_internet_and_ordinary_then_famous_and_internet_segments_come_first_in_list_order()
    {
        TagData ordinary = new("Nature", "outdoors");
        TagData famous = new("Emma Stone", "people > actress");
        TagData internetModel = new("SomeModel", "people > model");
        List<TagData> tags = [ordinary, famous, internetModel];

        var directory = WallpaperDirectoryResolver.Resolve(_layout, tags, _category);

        var expected = Path.Combine("/root/famous/L", _category.Name, famous.Tag, internetModel.Tag, ordinary.Tag);
        directory.ShouldBe(expected);
    }

    [Fact]
    public void when_multiple_ordinary_tags_have_categories_then_they_are_ordered_alphabetically_regardless_of_list_order()
    {
        List<TagData> tags = [new("Zebra", "animals"), new("Apple", "food"), new("Mango", "food")];

        var directory = WallpaperDirectoryResolver.Resolve(_layout, tags, _category);

        var expected = Path.Combine("/root/regular/L", _category.Name, "Apple", "Mango", "Zebra");
        directory.ShouldBe(expected);
    }

    [Fact]
    public void when_famous_and_ordinary_tags_are_mixed_then_famous_tags_keep_list_order_ahead_of_the_alphabetically_ordered_ordinary_tags()
    {
        TagData zebra = new("Zebra", "animals");
        TagData apple = new("Apple", "food");
        TagData famous = new("Emma Stone", "people > actress");
        List<TagData> tags = [zebra, famous, apple];

        var directory = WallpaperDirectoryResolver.Resolve(_layout, tags, _category);

        var expected = Path.Combine("/root/famous/L", _category.Name, famous.Tag, apple.Tag, zebra.Tag);
        directory.ShouldBe(expected);
    }

    [Fact]
    public void when_a_tag_contains_a_colon_then_the_colon_is_removed_from_the_path()
    {
        var directory = WallpaperDirectoryResolver.Resolve(_layout, [new TagData("Marvel: Avengers", "movies")], _category);

        directory.ShouldBe(Path.Combine("/root/regular", "L", _category.Name, "Marvel Avengers"));
    }

    [Fact]
    public void when_the_root_directory_starts_with_a_drive_letter_then_the_drive_colon_is_preserved()
    {
        var layout = new DirectoryLayout("C:/w", "/regular", "/famous");

        var directory = WallpaperDirectoryResolver.Resolve(layout, [], _category);

        directory.ShouldBe(Path.Combine("C:/w/regular", "L", _category.Name));
    }

    [Fact]
    public void when_a_tag_contains_an_invalid_path_character_then_the_character_is_removed()
    {
        var directory = WallpaperDirectoryResolver.Resolve(_layout, [new TagData("Bad\0Tag", "outdoors")], _category);

        directory.ShouldBe(Path.Combine("/root/regular", "L", _category.Name, "BadTag"));
    }

    [Fact]
    public void when_the_directory_layout_is_empty_and_the_category_name_is_short_then_the_short_path_is_still_resolved()
    {
        var layout = new DirectoryLayout(string.Empty, string.Empty, string.Empty);
        var category = new ScrapeCategory("A", "https://example.com/a");

        var directory = WallpaperDirectoryResolver.Resolve(layout, [], category);

        directory.ShouldBe(Path.Combine("A", "A"));
    }

    [Fact]
    public void when_more_than_six_eligible_tags_are_present_then_only_the_first_six_contribute_path_segments()
    {
        List<TagData> tags =
        [
            new("Emma Stone", "people > actress"),
            new("SomeModel", "people > model"),
            new("Apple", "food"),
            new("Banana", "food"),
            new("Cherry", "food"),
            new("Damson", "food"),
            new("Elderberry", "food"),
            new("Fig", "food")
        ];

        var directory = WallpaperDirectoryResolver.Resolve(_layout, tags, _category);

        var expected = Path.Combine("/root/famous/L", _category.Name, "Emma Stone", "SomeModel", "Apple", "Banana", "Cherry", "Damson");
        directory.ShouldBe(expected);
    }
}
