namespace Dsposal.Flow.Extensions;

public static class EnumerableExtensions
{
    
    public static IEnumerable<TResult> GetData<TSource, TResult, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TResult> selector,
        Func<TSource, TKey> keySelector,
        Func<TSource, bool>? filter = null,
        Func<TSource, TKey>? orderBySelector = null
    )
    {
        return source
            .DistinctBy(keySelector)
            .OrderBy(orderBySelector ?? keySelector)
            .Where(filter ?? (x => true))
            .Select(selector);
    }
}