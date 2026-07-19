using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Source.Generators.Attributes;

namespace AStar.Dev.Infrastructure.AppDb.ValueTypes;

/// <summary>
/// A strongly-typed identifier for an <see cref="EventEntity"/>.
/// </summary>
[StrongId(typeof(Guid))]
public readonly partial record struct FileEventId;
