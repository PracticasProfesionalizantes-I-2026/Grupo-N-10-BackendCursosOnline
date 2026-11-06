using Lumen.Shared.DTOs;
using Lumen.Shared.Exceptions;

namespace Lumen.API.Http;

internal static class ListPaging
{
    internal static ListPage? Read(int? limit, int? offset)
    {
        if (limit is null && offset is null) return null;
        if (limit is null or < 1 or > 100 || offset is < 0)
            throw new LumenValidationException("La paginación requiere limit entre 1 y 100 y offset mayor o igual a cero.");
        return new ListPage(offset ?? 0, limit.Value);
    }
}
