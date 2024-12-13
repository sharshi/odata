using Microsoft.AspNetCore.OData.Query;
using System.Text.RegularExpressions;

public static class QueryOptionsHelper
{
    public static long? GetId<T>(this ODataQueryOptions<T> queryOptions)
    {
        var value = queryOptions?.Apply?.RawValue ?? queryOptions?.Filter?.RawValue;

        if (value == null)
            return null;

        var filterString = value!;

        var match = Regex.Match(filterString, @"\b[Ii][Dd]\s+eq\s+(\d+)", RegexOptions.IgnoreCase);

        if (match.Success && long.TryParse(match.Groups[1].Value, out long id))
            return id;

        return null;
    }
}
