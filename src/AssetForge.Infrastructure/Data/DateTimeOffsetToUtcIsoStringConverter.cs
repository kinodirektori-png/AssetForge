using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AssetForge.Infrastructure.Data;

/// <summary>
/// SQLite cannot order or filter by <see cref="DateTimeOffset"/> in its default storage, so every timestamp is stored as
/// UTC text in ISO 8601 round-trip form. New values are written in one fixed form and share the same length and offset, so the text sorts by time. Reading is lenient, so a database written in an earlier form still opens.
/// </summary>
public sealed class DateTimeOffsetToUtcIsoStringConverter() : ValueConverter<DateTimeOffset, string>(
    v => v.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
    v => DateTimeOffset.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal));
