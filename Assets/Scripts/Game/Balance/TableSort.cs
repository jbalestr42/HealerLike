using System;
using System.Collections.Generic;
using System.Linq;

// Order of the rows of a table sorted on one of its columns (e.g. the Balance Report): the rows without a value
// (a null key, e.g. a wave never measured) always come last, whatever the direction, and equal rows keep their order
public static class TableSort
{
    public static List<T> Sort<T>(IEnumerable<T> rows, Func<T, IComparable> key, bool ascending)
    {
        List<T> list = rows.ToList();
        List<T> withValue = list.Where(row => key(row) != null).ToList();
        List<T> withoutValue = list.Where(row => key(row) == null).ToList();
        // OrderBy and OrderByDescending are stable
        IEnumerable<T> sorted = ascending
            ? withValue.OrderBy(key, Comparer<IComparable>.Default)
            : withValue.OrderByDescending(key, Comparer<IComparable>.Default);
        return sorted.Concat(withoutValue).ToList();
    }
}
