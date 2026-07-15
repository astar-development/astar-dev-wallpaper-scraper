
using System.Reflection;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AStar.Dev.Infrastructure.AppDb;

public static class ModelBuilderExtensions
{
    public static void UseSqliteFriendlyConversions(this ModelBuilder modelBuilder)
    {
        Type[] targetEntities =
        [
            typeof(AccountEntity),
            typeof(DriveStateEntity),
            typeof(FileClassificationCategoryEntity),
            typeof(FileClassificationEntity),
            typeof(SyncConflictEntity),
            typeof(SyncJobEntity),
            typeof(SyncConflictEntity),
            typeof(SyncedItemEntity),
            typeof(FileAccessDetailEntity),
            typeof(FileDetailEntity),
            typeof(ImageDetailEntity),
            typeof(DeletionStatusEntity),
            typeof(EventEntity),
            typeof(TagToIgnoreEntity),
            typeof(ModelToIgnoreEntity),
            typeof(ScrapedTagEntity),
            typeof(ConnectionStringsEntity),
            typeof(UserConfigurationEntity),
            typeof(SearchConfigurationEntity),
            typeof(SearchCategoryEntity),
            typeof(ScrapeDirectoriesEntity),
        ];

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(mutableEntityType => targetEntities.Contains(mutableEntityType.ClrType)))
        {
            ApplyConversionsForEntity(modelBuilder, entityType);
        }
    }

    private static void ApplyConversionsForEntity(ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        var entityTypeBuilder = modelBuilder.Entity(entityType.ClrType);

        foreach (var propInfo in entityType.ClrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propertyType = propInfo.PropertyType;

            if (propertyType == typeof(DateTimeOffset))
            {
                _ = entityTypeBuilder.Property(propInfo.Name).HasConversion(SqliteTypeConverters.DateTimeOffsetToTicks).HasColumnType("INTEGER").HasColumnName(propInfo.Name + "_Ticks");
            }
            else if (Nullable.GetUnderlyingType(propertyType) == typeof(DateTimeOffset))
            {
                _ = entityTypeBuilder.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableDateTimeOffsetToTicks).HasColumnType("INTEGER").HasColumnName(propInfo.Name + "_Ticks");
            }
            else if (propertyType == typeof(TimeSpan))
            {
                _ = entityTypeBuilder.Property(propInfo.Name).HasConversion(SqliteTypeConverters.TimeSpanToTicks).HasColumnType("INTEGER");
            }
            else if (Nullable.GetUnderlyingType(propertyType) == typeof(TimeSpan))
            {
                _ = entityTypeBuilder.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableTimeSpanToTicks).HasColumnType("INTEGER");
            }
            else if (propertyType == typeof(Guid))
            {
                _ = entityTypeBuilder.Property(propInfo.Name).HasConversion(SqliteTypeConverters.GuidToBytes).HasColumnType("BLOB");
            }
            else if (Nullable.GetUnderlyingType(propertyType) == typeof(Guid))
            {
                _ = entityTypeBuilder.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableGuidToBytes).HasColumnType("BLOB");
            }
            else if (propertyType == typeof(decimal))
            {
                _ = entityTypeBuilder.Property(propInfo.Name).HasConversion(SqliteTypeConverters.DecimalToCents).HasColumnType("INTEGER");
            }
            else if (Nullable.GetUnderlyingType(propertyType) == typeof(decimal))
            {
                _ = entityTypeBuilder.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableDecimalToCents).HasColumnType("INTEGER");
            }
            else if (propertyType.IsEnum)
            {
                _ = entityTypeBuilder.Property(propInfo.Name).HasConversion<int>().HasColumnType("INTEGER");
            }
            else if (Nullable.GetUnderlyingType(propertyType)?.IsEnum == true)
            {
                var enumType = Nullable.GetUnderlyingType(propertyType);
                if (enumType != null)
                {
                    var converterType = typeof(EnumToNumberConverter<,>).MakeGenericType(enumType, typeof(int));
                    var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                    _ = entityTypeBuilder.Property(propInfo.Name).HasConversion(converter).HasColumnType("INTEGER");
                }
            }
        }
    }
}
