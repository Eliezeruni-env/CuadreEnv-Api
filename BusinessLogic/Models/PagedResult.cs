using System.Collections.Generic;

namespace Onion.BussinesLogic.Models
{
    public record PagedResult<T>(IEnumerable<T> Items, int PageNumber, int PageSize, int TotalCount);
}
