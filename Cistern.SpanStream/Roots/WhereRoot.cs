﻿using Cistern.SpanStream.Utils;
using Cistern.Utils;

namespace Cistern.SpanStream.Roots;

public readonly struct WhereRoot<T>
    : IStreamNode<T>
{
    public readonly Func<T, bool> Predicate;

    public WhereRoot(Func<T, bool> predicate) => Predicate = predicate;

    int? IStreamNode<T>.TryGetSize(int sourceSize, out int upperBound)
    {
        upperBound = sourceSize;
        return null;
    }

    struct Execute
        : IExecuteIterator<T, T, Func<T, bool>>
    {
        TResult IExecuteIterator<T, T, Func<T, bool>>.Execute<TCurrent, TResult, TProcessStream>(ref Builder<TCurrent> builder, ref Span<T> span, in TProcessStream stream, in Func<T, bool> predicate)
        {
            var localCopy = stream;
            Iterator.Where(ref builder, span, ref localCopy, predicate);
            return localCopy.GetResult(ref builder);
        }
    }

    TResult IStreamNode<T>.Execute<TInitialDuplicate, TFinal, TResult, TProcessStream>(in ReadOnlySpan<TInitialDuplicate> spanAsSourceDuplicate, in TProcessStream processStream)
    {
        var span = Unsafe.SpanCast<TInitialDuplicate, T>(spanAsSourceDuplicate);

        return StackAllocator.Execute<T, T, TFinal, TResult, TProcessStream, Func<T, bool>, Execute>(0, ref span, in processStream, Predicate);
    }
}
