using System;
using System.Collections.Generic;
using System.Text;

namespace TRM.Core.Shared;

public static class EnumerableExtensions
{

    public static IEnumerable<(T First, T Second)> Pairwise<T>(this IList<T> source)
    {
        if (source == null || source.Count < 2)
            yield break;

        for (int i = 0; i < source.Count - 1; i++)
        {
            yield return (source[i], source[i + 1]);
        }
    }

}
