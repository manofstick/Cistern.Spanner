﻿using Cistern.SpanStream.Utils;

namespace Cistern.SpanStream.Roots;

public readonly struct SelectRoot<TSource, TNext>
    : IStreamNode<TNext>
{
    public Func<TSource, TNext> Selector { get; }

    public SelectRoot(Func<TSource, TNext> selector) =>
        (Selector) = (selector);

    TResult IStreamNode<TNext>.Execute<TSourceDuplicate, TResult, TProcessStream>(in ReadOnlySpan<TSourceDuplicate> spanAsSourceDuplicate, in TProcessStream processStream)
    {
        var span = Unsafe.SpanCast<TSourceDuplicate, TSource>(spanAsSourceDuplicate);

        var localCopy = processStream;
        Iterator.Select(span, ref localCopy, Selector);
        return localCopy.GetResult();
    }
}
