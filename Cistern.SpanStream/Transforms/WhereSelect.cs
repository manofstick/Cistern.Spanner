﻿using Cistern.Utils;
using System.Runtime.CompilerServices;

namespace Cistern.SpanStream.Transforms;

public readonly struct WhereSelect<TInput, TOutput, TPriorNode>
    : IStreamNode<TOutput>
    where TPriorNode : struct, IStreamNode<TInput>
{
    public readonly TPriorNode Node;
    private Func<TInput, bool> Predicate { get; }
    private Func<TInput, TOutput> Selector { get; }

    public WhereSelect(in TPriorNode nodeT, Func<TInput, bool> predicate, Func<TInput, TOutput> selector) =>
        (Node, Predicate, Selector) = (nodeT, predicate, selector);

    int? IStreamNode<TOutput>.TryGetSize(int sourceSize, out int upperBound)
    {
        Node.TryGetSize(sourceSize, out upperBound);
        return 0;
    }

    TResult IStreamNode<TOutput>.Execute<TTerminatorState, TInitialDuplicate, TFinal, TResult, TProcessStream>(in ReadOnlySpan<TInitialDuplicate> span, int? stackAllocationCount, in TProcessStream processStream) =>
        Node.Execute<TTerminatorState, TInitialDuplicate, TFinal, TResult, WhereSelectStream<TTerminatorState, TInput, TOutput, TFinal, TResult, TProcessStream>>(in span, stackAllocationCount, new(in processStream, Predicate, Selector));
}

struct WhereSelectStream<TTerminatorState, TInput, TOutput, TFinal, TResult, TProcessStream>
    : IProcessStream<TTerminatorState, TInput, TFinal, TResult>
    where TProcessStream : struct, IProcessStream<TTerminatorState, TOutput, TFinal, TResult>
{
    /* can't be readonly */ TProcessStream _next;
    readonly Func<TInput, bool> _predicate;
    readonly Func<TInput, TOutput> _selector;

    public WhereSelectStream(in TProcessStream nextProcessStream, Func<TInput, bool> predicate, Func<TInput, TOutput> selector) =>
        (_next, _predicate, _selector) = (nextProcessStream, predicate, selector);

    TResult IProcessStream<TTerminatorState, TInput, TFinal, TResult>.GetResult(ref StreamState<TFinal, TTerminatorState> state) => _next.GetResult(ref state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IProcessStream<TTerminatorState, TInput, TFinal>.ProcessNext(ref StreamState<TFinal, TTerminatorState> state, in TInput input) =>
        !_predicate(input) || _next.ProcessNext(ref state, _selector(input));
}
