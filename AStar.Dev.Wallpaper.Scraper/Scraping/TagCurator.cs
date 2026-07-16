namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Filters a wallpaper's raw tags against the models-to-ignore and tags-to-ignore lists.
/// </summary>
public static class TagCurator
{
    /// <summary>
    ///     Curates the supplied tags, dropping any that match either ignore list.
    /// </summary>
    /// <param name="tags">The raw tags read from the wallpaper page.</param>
    /// <param name="modelsToIgnore">Model names whose wallpapers should not be saved.</param>
    /// <param name="tagsToIgnore">Tag names that should be dropped from a wallpaper's tag list.</param>
    public static TagCuration Curate(IReadOnlyList<TagData> tags, IReadOnlyList<string> modelsToIgnore, IReadOnlyList<string> tagsToIgnore)
    {
        List<TagData> kept = [];
        List<string>  messages = [];

        for (int i = 0; i < tags.Count; i++)
        {
            var tag = tags[i];

            if (tag.Tag.Equals("model", StringComparison.OrdinalIgnoreCase)) continue;
            if (tag.Tag.EndsWith(" model", StringComparison.OrdinalIgnoreCase)) tag = tag with { Tag = tag.Tag[..^" model".Length] };
            messages.Add($"Found tag: {tag.Tag}, category: {tag.Category}, isFamous: {tag.IsFamous}, isInternet: {tag.IsInternet}");

            if (ModelOrTagToIgnore(modelsToIgnore, tagsToIgnore, tag)) continue;

            kept.Add(tag);
            messages.Add($"Tag: {tag.Tag} is not in the tagsToIgnore list, added to the list of tags to save to the database");
        }

        return new TagCuration(kept, messages);
    }

    private static bool ModelOrTagToIgnore(IReadOnlyList<string> modelsToIgnore, IReadOnlyList<string> tagsToIgnore, TagData tag)
        => modelsToIgnore.Any(model => TagCheck(tag.Tag, model)) || tagsToIgnore.Any(ignoredTag => TagCheck(tag.Tag, ignoredTag));

    private static bool TagCheck(string category, string contains)
        => category.Contains(contains, StringComparison.OrdinalIgnoreCase);
}
