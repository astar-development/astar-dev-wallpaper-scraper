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

        foreach (var tag in tags)
        {
            messages.Add($"Found tag: {tag.Tag}, category: {tag.Category}, isFamous: {tag.IsFamous}, isInternet: {tag.IsInternet}");

            if (modelsToIgnore.Any(model => model.Equals(tag.Tag, StringComparison.OrdinalIgnoreCase)))
            {
                messages.Add($"Ignoring model: {tag.Tag} as it is in the modelsToIgnore list");

                continue;
            }

            messages.Add($"Model: {tag.Tag} is not in the modelsToIgnore list, we should save it to the database");

            if (tagsToIgnore.Any(ignoredTag => ignoredTag.Equals(tag.Tag, StringComparison.OrdinalIgnoreCase)))
            {
                messages.Add($"Ignoring tag: {tag.Tag} as it is in the tagsToIgnore list");

                continue;
            }

            kept.Add(tag);
            messages.Add($"Tag: {tag.Tag} is not in the tagsToIgnore list, added to the list of tags to save to the database");
        }

        return new TagCuration(kept, messages);
    }
}
