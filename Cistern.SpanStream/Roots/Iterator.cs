﻿namespace Cistern.SpanStream.Roots
{
    internal static class Iterator
    {
        public static void Vanilla<TSource, TProcessStream>(Span<TSource> span, ref TProcessStream stream)
            where TProcessStream : struct, IProcessStream<TSource>
        {
            for (var i = 0; i < span.Length; ++i)
            {
                if (!stream.ProcessNext(in span[i]))
                    break;
            }
        }

        internal static void Where<TSource, TProcessStream>(Span<TSource> span, ref TProcessStream stream, Func<TSource, bool> predicate)
            where TProcessStream : struct, IProcessStream<TSource>
        {
            for (var i = 0; i < span.Length; ++i)
            {
                if (predicate(span[i]))
                {
                    if (!stream.ProcessNext(in span[i]))
                        break;
                }
            }
        }

        internal static void SelectWhere<TSource, TNext, TProcessStream>(Span<TSource> span, ref TProcessStream stream, Func<TSource, TNext> selector, Func<TNext, bool> predicate)
            where TProcessStream : struct, IProcessStream<TNext>
        {
            for (var i = 0; i < span.Length; ++i)
            {
                var next = selector(span[i]);
                if (predicate(next))
                {
                    if (!stream.ProcessNext(next))
                        break;
                }
            }
        }

        internal static void Select<TSource, TNext, TProcessStream>(Span<TSource> span, ref TProcessStream stream, Func<TSource, TNext> selector)
            where TProcessStream : struct, IProcessStream<TNext>
        {
            for (var i = 0; i < span.Length; ++i)
            {
                if (!stream.ProcessNext(selector(span[i])))
                    break;
            }
        }

        internal static void WhereSelect<TSource, TNext, TProcessStream>(Span<TSource> span, ref TProcessStream stream, Func<TSource, bool> predicate, Func<TSource, TNext> selector)
            where TProcessStream : struct, IProcessStream<TNext>
        {
            for (var i = 0; i < span.Length; ++i)
            {
                if (predicate(span[i]))
                {
                    if (!stream.ProcessNext(selector(span[i])))
                        break;
                }
            }
        }
    }
}
