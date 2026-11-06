using System.Globalization;
using Lumen.Shared.DTOs;
using Lumen.Shared.Exceptions;
using Microsoft.Net.Http.Headers;

namespace Lumen.API.Http;

internal static class CourseEditTag
{
    internal static string For(CourseResponseDTO course) => Format(course.Id, course.UpdatedAtUtc.Ticks);

    internal static IReadOnlyCollection<long> Read(Guid courseId, IList<string> header)
    {
        if (header.Count == 0) throw new CourseEditPreconditionRequiredException();
        if (!EntityTagHeaderValue.TryParseStrictList(header, out var tags) || tags.Count == 0 ||
            (tags.Count > 1 && tags.Contains(EntityTagHeaderValue.Any)))
        {
            throw new LumenValidationException("La cabecera If-Match no es válida.");
        }
        if (tags.Count == 1 && tags[0].Equals(EntityTagHeaderValue.Any))
            throw new CourseEditPreconditionRequiredException();

        var versions = new List<long>();
        foreach (var tag in tags)
        {
            var value = tag.Tag.Value;
            if (!tag.IsWeak && value is { Length: 51 } &&
                long.TryParse(value.AsSpan(34, 16), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var ticks) &&
                value == Format(courseId, ticks))
            {
                versions.Add(ticks);
            }
        }
        return versions;
    }

    private static string Format(Guid id, long ticks) => $"\"{id:N}.{ticks:x16}\"";
}
