using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SmartSis.Api.Shared.Data;

/// <summary>
/// All <c>DateTime</c> columns hold UTC (backend.md). SQL Server's <c>datetime2</c> doesn't store the kind, so values come
/// back as <see cref="DateTimeKind.Unspecified"/> and System.Text.Json writes them without the trailing "Z": the browser
/// then reads them as local time. Marking them UTC on read makes every response end in "Z". Writes are unchanged.
/// </summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v,
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
