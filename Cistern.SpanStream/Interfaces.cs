﻿using Cistern.Utils;

namespace Cistern.SpanStream;

public interface IProcessStream<TElement, TFinal>
{
    public bool ProcessNext(ref StreamState<TFinal> builder, in TElement input);
}

public interface IProcessStream<TInput, TFinal, TResult>
    : IProcessStream<TInput, TFinal>
{
    TResult GetResult(ref StreamState<TFinal> builder);
}

public interface IStreamNode<TInput>
{
    int? TryGetSize(int sourceSize, out int upperBound);
    TResult Execute<TInitialDuplicate, TFinal, TResult, TNextInChain>(in ReadOnlySpan<TInitialDuplicate> span, int? stackAllocationCount, in TNextInChain fenum)
        where TNextInChain : struct, IProcessStream<TInput, TFinal, TResult>;
}
