using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Scraping;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenWallpaperDirectoryResolver
{
    private static readonly DirectoryLayout layout = new("/root", "/regular", "/famous");
    private static readonly ScrapeCategory category = new("Landscapes", "https://example.com/landscapes", false, false);

    [Fact]
    public void when_no_tags_are_famous_then_the_regular_base_directory_is_used()
    {
        var directory = WallpaperDirectoryResolver.Resolve(layout, [new TagData("Nature", "outdoors")], category, [new FileClassificationCategoryEntity { Name = "Nature", IncludeInSearch = true, Level = 1 }]);

        directory.ShouldBe("/root/regular/L/Landscapes/Nature");
    }

    [Fact]
    public void when_any_tag_is_famous_then_the_famous_base_directory_is_used_instead()
    {
        List<TagData> tags = [new("Emma Stone", "people > actress")];

        var directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, [new FileClassificationCategoryEntity { Name = "Emma Stone", IncludeInSearch = true, Level = 1 }]);

        directory.ShouldStartWith("/root/famous/L/Landscapes");
    }

    [Fact]
    public void when_there_are_no_tags_then_only_the_root_base_and_category_directories_are_returned()
    {
        var directory = WallpaperDirectoryResolver.Resolve(layout, [], category, []);

        directory.ShouldBe("/root/regular/L/Landscapes");
    }

    [Fact]
    public void when_a_tag_has_no_category_then_it_contributes_no_path_segment()
    {
        var directory = WallpaperDirectoryResolver.Resolve(layout, [new TagData("Untagged", null)], category, []);

        directory.ShouldBe("/root/regular/L/Landscapes");
    }

    [Fact]
    public void when_a_tag_matches_the_category_name_then_it_contributes_no_path_segment()
    {
        var directory = WallpaperDirectoryResolver.Resolve(layout, [new TagData(category.Name, "outdoors")], category, []);

        directory.ShouldBe("/root/regular/L/Landscapes");
    }

    [Fact]
    public void when_a_tag_matches_the_category_name_with_different_casing_then_it_contributes_no_path_segment()
    {
        var directory = WallpaperDirectoryResolver.Resolve(layout, [new TagData(category.Name.ToUpperInvariant(), "outdoors")], category, []);

        directory.ShouldBe("/root/regular/L/Landscapes");
    }

    [Fact]
    public void when_tags_are_a_mix_of_famous_internet_and_ordinary_then_famous_and_internet_segments_come_first_in_list_order()
    {
        TagData ordinary = new("Nature", "outdoors");
        TagData famous = new("Emma Stone", "people > actress");
        TagData internetModel = new("SomeModel", "people > model");
        List<TagData> tags = [ordinary, famous, internetModel];

        var directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, [new FileClassificationCategoryEntity { Name = famous.Tag, IncludeInSearch = true, Level = 1 }, new FileClassificationCategoryEntity { Name = internetModel.Tag, IncludeInSearch = true, Level = 1 }, new FileClassificationCategoryEntity { Name = ordinary.Tag, IncludeInSearch = true, Level = 1 }]);

        directory.ShouldBe("/root/famous/L/Landscapes/Emma Stone/SomeModel/Nature");
    }

    [Fact]
    public void when_multiple_ordinary_tags_have_categories_then_they_are_ordered_alphabetically_regardless_of_list_order()
    {
        List<TagData> tags = [new("Zebra", "animals"), new("Apple", "food"), new("Mango", "food")];

        var directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, [ new FileClassificationCategoryEntity { Name = "Apple", IncludeInSearch = true, Level = 1 }, new FileClassificationCategoryEntity { Name = "Mango", IncludeInSearch = true, Level = 1 }, new FileClassificationCategoryEntity { Name = "Zebra", IncludeInSearch = true, Level = 1 }]);

        directory.ShouldBe("/root/regular/L/Landscapes/Apple/Mango/Zebra");
    }

    [Fact]
    public void when_famous_and_ordinary_tags_are_mixed_then_famous_tags_keep_list_order_ahead_of_the_alphabetically_ordered_ordinary_tags()
    {
        TagData zebra = new("Zebra", "animals");
        TagData apple = new("Apple", "food");
        TagData famous = new("Emma Stone", "people > actress");
        List<TagData> tags = [zebra, famous, apple];

        var directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, [

            new FileClassificationCategoryEntity { Name = famous.Tag, IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = apple.Tag, IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = zebra.Tag, IncludeInSearch = true, Level = 1 }]);

        directory.ShouldBe("/root/famous/L/Landscapes/Emma Stone/Apple/Zebra");
    }

    [Fact]
    public void when_a_tag_contains_a_colon_then_the_colon_is_removed_from_the_path()
    {
        var directory = WallpaperDirectoryResolver.Resolve(layout, [new TagData("Marvel: Avengers", "movies")], category, [ new FileClassificationCategoryEntity { Name = "Marvel: Avengers", IncludeInSearch = true, Level = 1 }]);

        directory.ShouldBe("/root/regular/L/Landscapes/Marvel Avengers");
    }

    [Fact]
    public void when_the_root_directory_starts_with_a_drive_letter_then_the_drive_colon_is_preserved()
    {
        var layout = new DirectoryLayout("C:/w", "/regular", "/famous");

        var directory = WallpaperDirectoryResolver.Resolve(layout, [], category, []);

        directory.ShouldBe("C:/w/regular/L/Landscapes");
    }

    [Fact]
    public void when_a_tag_contains_an_invalid_path_character_then_the_character_is_removed()
    {
        var directory = WallpaperDirectoryResolver.Resolve(layout, [new TagData("Bad\0Tag", "outdoors")], category, [ new FileClassificationCategoryEntity { Name = "Bad\0Tag", IncludeInSearch = true, Level = 1 }]);

        directory.ShouldBe("/root/regular/L/Landscapes/Bad Tag");
    }

    [Fact]
    public void when_the_directory_layout_is_empty_and_the_category_name_is_short_then_the_short_path_is_still_resolved()
    {
        var layout = new DirectoryLayout(string.Empty, string.Empty, string.Empty);
        var category = new ScrapeCategory("A", "https://example.com/a", false, false);

        var directory = WallpaperDirectoryResolver.Resolve(layout, [], category, []);

        directory.ShouldBe("A/A");
    }

    [Fact]
    public void when_more_than_twelve_eligible_tags_are_present_then_only_the_first_twelve_contribute_path_segments()
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
            new("Fig", "food"),
            new("Grape", "food"),
            new("Honeydew", "food"),
            new("Iced Tea", "food"),
            new("Jackfruit", "food"),
            new("Kiwi", "food")
        ];

        var directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, [ new FileClassificationCategoryEntity { Name = "Emma Stone", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "SomeModel", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Apple", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Banana", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Cherry", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Damson", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Elderberry", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Fig", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Grape", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Honeydew", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Iced Tea", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Jackfruit", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Kiwi", IncludeInSearch = true, Level = 1 }
        ]);

        directory.ShouldBe("/root/famous/L/Landscapes/Emma Stone/SomeModel/Apple/Banana/Cherry/Damson/Elderberry/Fig/Grape/Honeydew/Iced Tea/Jackfruit");
    }

    [Fact]
    public void when_tags_include_duplicates_then_only_one_path_segment_is_created()
    {
        TagData ordinary = new("Nature", "outdoors");
        TagData famous = new("Emma Stone", "people > actress");
        TagData internetModel = new("SomeModel", "people > model");
        TagData ordinary2 = new("Nature", "outdoors");
        TagData famous2 = new("Emma Stone", "people > actress");
        TagData internetModel2 = new("SomeModel", "people > model");
        List<TagData> tags = [ordinary, internetModel2, internetModel, ordinary2, ordinary2, ordinary2, famous2, famous];

        var directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, [new FileClassificationCategoryEntity { Name = famous.Tag, IncludeInSearch = true, Level = 1 }, new FileClassificationCategoryEntity { Name = internetModel.Tag, IncludeInSearch = true, Level = 1 }, new FileClassificationCategoryEntity { Name = ordinary.Tag, IncludeInSearch = true, Level = 1 }]);

        directory.ShouldBe("/root/famous/L/Landscapes/Emma Stone/SomeModel/Nature");
    }
}
