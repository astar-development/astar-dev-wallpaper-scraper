using AStar.Dev.Wallpaper.Scraper.Scraping;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenWallpaperDirectoryResolver
{
    private static readonly DirectoryLayout _layout = new("/root", "/regular", "/famous");

    [Fact]
    public void when_no_tags_are_famous_then_the_regular_base_directory_is_used()
    {
        var directory = WallpaperDirectoryResolver.Resolve(_layout, [new TagData("Nature", "outdoors")]);

        directory.ShouldBe(Path.Combine("/root/regular", "Nature"));
    }

    [Fact]
    public void when_any_tag_is_famous_then_the_famous_base_directory_is_used_instead()
    {
        List<TagData> tags = [new("Emma Stone", "people > actress")];

        var directory = WallpaperDirectoryResolver.Resolve(_layout, tags);

        directory.ShouldStartWith("/root/famous");
    }

    [Fact]
    public void when_there_are_no_tags_then_only_the_root_and_base_directory_are_returned()
    {
        var directory = WallpaperDirectoryResolver.Resolve(_layout, []);

        directory.ShouldBe("/root/regular");
    }

    [Fact]
    public void when_a_tag_has_no_category_then_it_contributes_no_path_segment()
    {
        var directory = WallpaperDirectoryResolver.Resolve(_layout, [new TagData("Untagged", null)]);

        directory.ShouldBe("/root/regular");
    }

    [Fact]
    public void when_tags_are_a_mix_of_famous_internet_and_ordinary_then_famous_and_internet_segments_come_first_in_list_order()
    {
        TagData ordinary = new("Nature", "outdoors");
        TagData famous = new("Emma Stone", "people > actress");
        TagData internetModel = new("SomeModel", "people > model");
        List<TagData> tags = [ordinary, famous, internetModel];

        var directory = WallpaperDirectoryResolver.Resolve(_layout, tags);

        var expected = Path.Combine(Path.Combine(Path.Combine("/root/famous", famous.Tag), internetModel.Tag), ordinary.Tag);
        directory.ShouldBe(expected);
    }

    [Fact]
    public void when_multiple_ordinary_tags_have_categories_then_they_are_ordered_alphabetically_regardless_of_list_order()
    {
        List<TagData> tags = [new("Zebra", "animals"), new("Apple", "food"), new("Mango", "food")];

        var directory = WallpaperDirectoryResolver.Resolve(_layout, tags);

        var expected = Path.Combine(Path.Combine(Path.Combine("/root/regular", "Apple"), "Mango"), "Zebra");
        directory.ShouldBe(expected);
    }

    [Fact]
    public void when_famous_and_ordinary_tags_are_mixed_then_famous_tags_keep_list_order_ahead_of_the_alphabetically_ordered_ordinary_tags()
    {
        TagData zebra = new("Zebra", "animals");
        TagData apple = new("Apple", "food");
        TagData famous = new("Emma Stone", "people > actress");
        List<TagData> tags = [zebra, famous, apple];

        var directory = WallpaperDirectoryResolver.Resolve(_layout, tags);

        var expected = Path.Combine(Path.Combine(Path.Combine("/root/famous", famous.Tag), apple.Tag), zebra.Tag);
        directory.ShouldBe(expected);
    }
}
