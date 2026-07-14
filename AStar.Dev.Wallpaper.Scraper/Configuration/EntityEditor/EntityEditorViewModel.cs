using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO.Abstractions;
using System.Reactive.Linq;
using System.Windows.Input;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Utilities;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;

/// <summary>
///     Generic CRUD + JSON import/export editor for a single database table, driven entirely by an
///     <see cref="EntityEditorDescriptor{TEntity}" />.
/// </summary>
/// <typeparam name="TEntity">The entity type backing the table being edited.</typeparam>
public sealed class EntityEditorViewModel<TEntity> : EntityEditorViewModelBase, IDisposable
    where TEntity : class
{
    private readonly AppDbContext context;
    private readonly EntityEditorDescriptor<TEntity> descriptor;
    private readonly IFileSystem fileSystem;
    private readonly string exportDirectory;
    private readonly string exportFilePath;
    private readonly ObservableCollection<TEntity> items;
    private TEntity? selectedItem;

    /// <summary>
    ///     Initialises a new editor, eagerly loading every row of the table described by <paramref name="descriptor" />
    ///     via a dedicated <see cref="AppDbContext" /> that is kept open for the lifetime of this view model.
    /// </summary>
    /// <param name="dbContextFactory">Used to create the dedicated context this editor tracks changes against.</param>
    /// <param name="descriptor">Describes the table, its columns and its behaviour.</param>
    /// <param name="fileSystem">Used for JSON import/export so tests can substitute a fake file system.</param>
    /// <param name="exportDirectory">The directory Import/Export read from and write to.</param>
    public EntityEditorViewModel(IDbContextFactory<AppDbContext> dbContextFactory, EntityEditorDescriptor<TEntity> descriptor, IFileSystem fileSystem, string exportDirectory)
    {
        this.descriptor = descriptor;
        this.fileSystem = fileSystem;
        this.exportDirectory = exportDirectory;
        exportFilePath = fileSystem.Path.Combine(exportDirectory, $"{descriptor.TableName}.json");
        context = dbContextFactory.CreateDbContext();
        items = new ObservableCollection<TEntity>(context.Set<TEntity>().ToList());

        var canAdd = descriptor.AllowAddRemove
            ? Observable.Return(true)
            : Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                    handler => items.CollectionChanged += handler,
                    handler => items.CollectionChanged -= handler)
                .Select(_ => items.Count == 0)
                .StartWith(items.Count == 0);

        AddCommand = ReactiveCommand.Create(AddRow, canAdd);
        DeleteCommand = ReactiveCommand.Create(DeleteSelectedRow, this.WhenAnyValue(vm => vm.SelectedItem).Select(item => item is not null));
        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
        ImportCommand = ReactiveCommand.CreateFromTask(ImportAsync);
        ExportCommand = ReactiveCommand.CreateFromTask(ExportAsync);
    }

    /// <inheritdoc />
    public override string Title => descriptor.DisplayName;

    /// <inheritdoc />
    public override IList Items => items;

    /// <inheritdoc />
    public override bool CanAddRemove => descriptor.AllowAddRemove;

    /// <inheritdoc />
    public override object? SelectedItem
    {
        get => selectedItem;
        set => this.RaiseAndSetIfChanged(ref selectedItem, (TEntity?)value);
    }

    /// <inheritdoc />
    public override ICommand AddCommand { get; }

    /// <inheritdoc />
    public override ICommand DeleteCommand { get; }

    /// <inheritdoc />
    public override ICommand SaveCommand { get; }

    /// <inheritdoc />
    public override ICommand ImportCommand { get; }

    /// <inheritdoc />
    public override ICommand ExportCommand { get; }

    /// <inheritdoc />
    public override bool IsColumnVisible(string propertyName) =>
        !descriptor.ExcludedColumns.Contains(propertyName);

    /// <inheritdoc />
    public override bool IsColumnReadOnly(string propertyName) =>
        descriptor.ReadOnlyColumns.Contains(propertyName);

    /// <summary>Disposes the dedicated <see cref="AppDbContext" /> held for the lifetime of this editor.</summary>
    public void Dispose() =>
        context.Dispose();

    private void AddRow()
    {
        var entity = descriptor.CreateNew();
        context.Add(entity);
        items.Add(entity);
    }

    private void DeleteSelectedRow()
    {
        if (selectedItem is null)
        {
            return;
        }

        context.Remove(selectedItem);
        items.Remove(selectedItem);
        SelectedItem = null;
    }

    private async Task SaveAsync()
    {
        try
        {
            if (descriptor.OnBeforeAddAsync is not null)
            {
                var addedEntities = context.ChangeTracker.Entries<TEntity>()
                    .Where(entry => entry.State == EntityState.Added)
                    .Select(entry => entry.Entity)
                    .ToList();

                foreach (var entity in addedEntities)
                {
                    await descriptor.OnBeforeAddAsync(context, entity, CancellationToken.None);
                }
            }

            await context.SaveChangesAsync();
            StatusMessage = $"Saved {items.Count} row(s).";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Save failed: {exception.Message}";
        }
    }

    private Task ExportAsync()
    {
        try
        {
            fileSystem.Directory.CreateDirectory(exportDirectory);
            fileSystem.File.WriteAllText(exportFilePath, items.ToList().ToJson());
            StatusMessage = $"Exported {items.Count} row(s) to {exportFilePath}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Export failed: {exception.Message}";
        }

        return Task.CompletedTask;
    }

    private Task ImportAsync()
    {
        try
        {
            if (!fileSystem.File.Exists(exportFilePath))
            {
                StatusMessage = $"Import failed: {exportFilePath} was not found.";

                return Task.CompletedTask;
            }

            var imported = fileSystem.File.ReadAllText(exportFilePath).FromJson<List<TEntity>>(Constants.WebDeserialisationSettings);

            foreach (var existing in context.Set<TEntity>().Local.ToList())
            {
                context.Remove(existing);
            }

            context.AddRange(imported);
            items.Clear();

            foreach (var entity in imported)
            {
                items.Add(entity);
            }

            StatusMessage = $"Imported {imported.Count} row(s) from {exportFilePath}. Click Save to persist.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Import failed: {exception.Message}";
        }

        return Task.CompletedTask;
    }
}
