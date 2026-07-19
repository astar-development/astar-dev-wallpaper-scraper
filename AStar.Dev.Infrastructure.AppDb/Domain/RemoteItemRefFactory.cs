using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Infrastructure.AppDb.ValueTypes;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Factory for <see cref="RemoteItemRef"/>.</summary>
public static class RemoteItemRefFactory
{
    /// <summary>Creates a <see cref="RemoteItemRef"/> identifying a specific remote drive item.</summary>
    public static RemoteItemRef Create(AccountId accountId, OneDriveFolderId folderId, OneDriveItemId remoteItemId) => new(accountId, folderId, remoteItemId);
}
