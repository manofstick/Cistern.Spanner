﻿using Cistern.Spanner.Roots;
using Cistern.Spanner.Terminators;
using Cistern.Spanner.Utils;
using Cistern.Utils;
using System.Buffers;
using System.Xml.Linq;

namespace Cistern.Spanner.Transforms;

public /*readonly*/ struct Reverse<TInitial, TInput, TPriorNode>
    : IStreamNode<TInitial, TInput>
    where TPriorNode : struct, IStreamNode<TInitial, TInput>
{
    internal /*readonly*/ TPriorNode Node;

    private int _stackElementCount;
    private ArrayPool<TInput>? _maybeArrayPool;

    private int _index;
    private TInput[] _reversed;

    public Reverse(ref TPriorNode nodeT, int? stackElementCount, ArrayPool<TInput>? maybeArrayPool)
    {
        Node = nodeT;
        _stackElementCount = stackElementCount ?? 0;
        _maybeArrayPool = maybeArrayPool;
        _reversed = UninitializedArrayStandIn<TInput>.Instance;
        _index = int.MaxValue;
    }

    int? IStreamNode<TInitial, TInput>.TryGetSize(int sourceSize, out int upperBound) =>
        Node.TryGetSize(sourceSize, out upperBound);

    public TResult Execute<TFinal, TResult, TProcessStream, TContext>(in TProcessStream processStream, in ReadOnlySpan<TInitial> span, int? stackAllocationCount)
        where TProcessStream : struct, IProcessStream<TInput, TFinal, TResult>
    {
        var _ = Node.TryGetSize(span.Length, out var upperBound);
        if (upperBound <= _stackElementCount)
        {
            return Node.Execute<TInput, TResult, ReverseOnStreamState<TInput, TFinal, TResult, TProcessStream, TContext>, TContext>(new(in processStream, upperBound), span, upperBound);
        }
        else
        {
            var reversedArray = CreateReversedArray(span);
            return Root<TInput>.Instance.Execute<TFinal, TResult, TProcessStream, TContext>(processStream, reversedArray.ToReadOnlySpan(), 0);
        }
    }

    public bool TryGetNext(ref EnumeratorState<TInitial> state, out TInput current)
    {
        if (_index < _reversed.Length)
        {
            current = _reversed[_index++];
            return true;
        }
        return LesserPath(ref state, out current);
    }

    bool LesserPath(ref EnumeratorState<TInitial> state, out TInput current)
    {
        if (UninitializedArrayStandIn.IsArrayUninitialized(_reversed))
        {
            _index = 0;
            var span = state.Span[state.Index..];
            _reversed = CreateReversedArray(in span);
            state.Index = state.Span.Length;
            return TryGetNext(ref state, out current);
        }
        current = default!;
        return false;
    }

    private TInput[] CreateReversedArray(in ReadOnlySpan<TInitial> span)
    {
        var reversed = ToArray<TInput>.Execute<TInitial, TPriorNode, object>(span, ref Node, _stackElementCount, _maybeArrayPool);
        Array.Reverse(reversed);
        return reversed;
    }
}

public struct ReverseOnStreamState<TInput, TFinal, TResult, TProcessStream, TContext>
    : IProcessStream<TInput, TInput, TResult>
        where TProcessStream : struct, IProcessStream<TInput, TFinal, TResult>
{
    TProcessStream _processStream;
    int _index;
    readonly int _size;

    public ReverseOnStreamState(in TProcessStream processStream, int size) =>
        (_processStream, _size, _index) = (processStream, size, size);

    public TResult GetResult(ref StreamState<TInput> builder) =>
        Root<TInput>.Instance.Execute<TFinal, TResult, TProcessStream, TContext>(_processStream, builder.Current[_index.._size], 0);

    public bool ProcessNext(ref StreamState<TInput> builder, in TInput input)
    {
        builder.Current[--_index] = input;
        return true;
    }
}
