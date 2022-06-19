﻿using Cistern.SpanStream;
using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Cistern.Utils;

internal static class StackAllocator
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BufferStorage<T>
    {
        public T[] _01;
        public T[] _02;
        public T[] _03;
        public T[] _04;
        public T[] _05;
        public T[] _06;
        public T[] _07;
        public T[] _08;
        public T[] _09;
        public T[] _10;
        public T[] _11;
        public T[] _12;
        public T[] _13;
        public T[] _14;
        public T[] _15;
        public T[] _16;
        public T[] _17;
        public T[] _18;
        public T[] _19;
        public T[] _20;
        public T[] _21;
        public T[] _22;
        public T[] _23;
        public T[] _24;
        public T[] _25;
        public T[] _26;
        public T[] _27;
        public T[] _28;
        public T[] _29;
        public T[] _30;

        public const int NumberOfElements = 30;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SequentialDataPair<T>
    {
        public T Item1;
        public T Item2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct MemoryChunk<T, TChunk>
        where TChunk : struct
    {
        public T Head;
        public TChunk Tail;
    }

    static void Allocate<T, Chunk>(int requiredSize, int currentSize)
        where Chunk : struct
    {
        MemoryChunk<T, Chunk> chunkOfStackSpace = default;

        var span = MemoryMarshal.CreateSpan(ref chunkOfStackSpace.Head, currentSize);
    }

    static void BuildStackObject<T, Chunk>(int requiredSize, int currentSize)
        where Chunk:struct
    {
        var nextSizeUp = ((currentSize - 1) * 2) + 1;

        if (currentSize < requiredSize)
            BuildStackObject<T, SequentialDataPair<Chunk>>(requiredSize, nextSizeUp);
        else
            Allocate<T, Chunk>(requiredSize, currentSize);
    }

    public static TResult Execute<TSource, TNext, TCurrent, TResult, TProcessStream, TState, TExecution>(int requiredSize, ref Span<TSource> span, in TProcessStream stream, in TState state)
        where TProcessStream : struct, IProcessStream<TNext, TCurrent, TResult>
        where TExecution : struct, IExecuteIterator<TSource, TNext, TState>
    {
        Builder<TCurrent> builder = default;

        return default(TExecution).Execute<TCurrent, TResult, TProcessStream>(ref builder, ref span, in stream, in state);
    }
}


public ref struct Builder<T>
{
    readonly ArrayPool<T>? _maybePool;
    readonly Span<T> _root;
    readonly int? _upperBound;

    readonly Span<T[]?> _buffers;
    int _bufferCount;

    Span<T> _current;
    int _nextIdx;

    int _count;

    public Builder(ArrayPool<T>? maybePool, Span<T[]?> bufferStore, Span<T> initalBuffer, int? upperBound)
    {
        _maybePool = maybePool;
        _upperBound = upperBound;

        _root = initalBuffer;
        _current = initalBuffer;
        _buffers = bufferStore;

        _bufferCount = 0;
        _nextIdx = 0;
        _count = 0;
    }

    public void Dispose()
    {
        if (_maybePool == null)
            return;

        for (var idx = 0; idx < _bufferCount; ++idx)
            _maybePool.Return(_buffers[idx]!);
    }

    public void Add(T item)
    {
        if (_nextIdx == _current.Length)
            AllocateNext();

        _current[_nextIdx] = item;

        ++_count;
        ++_nextIdx;
    }
    private void AllocateNext()
    {
        var nextSize = _current.Length * 2;
        if (_count + nextSize > _upperBound)
        {
            nextSize = _upperBound.Value - _count;
            if (nextSize <= 0)
                throw new IndexOutOfRangeException("Enumerator length has exceeded original count");
        }

        var newArray =
            _maybePool == null
                ? new T[nextSize]
                : _maybePool.Rent(nextSize);
        _buffers[_bufferCount++] = newArray;
        _current = newArray.AsSpan();
        _nextIdx = 0;
    }

    public T[] ToArray()
    {
        if (_count == 0)
            return Array.Empty<T>();

        var array = new T[_count];

        var ptr = array.AsSpan();
        if (_bufferCount == 0)
        {
            _root[.._count].CopyTo(ptr);
        }
        else
        {
            _root.CopyTo(ptr);
            ptr = ptr[_root.Length..];
            for (var idx = 0; idx < _bufferCount - 1; ++idx)
            {
                var buffer = _buffers[idx].AsSpan();
                buffer.CopyTo(ptr);
                ptr = ptr[buffer.Length..];
            }
            _buffers[_bufferCount - 1].AsSpan(0, _nextIdx).CopyTo(ptr);
        }

        return array;
    }

    public ImmutableArray<T> ToImmutableArray()
    {
        if (_count == 0)
            return ImmutableArray<T>.Empty;

        var array = ImmutableArray.CreateBuilder<T>(_count);

        var head = _root[..Math.Min(_count, _root.Length)];
        foreach (var item in head)
            array.Add(item);
        for (var idx = 0; idx < _bufferCount - 1; ++idx)
            array.AddRange(_buffers[idx]!);
        if (_bufferCount > 0)
        { 
            var tail =
                _buffers[_bufferCount - 1]
                .AsSpan(0, _nextIdx);
            foreach (var item in tail)
                array.Add(item);
        }

        return array.MoveToImmutable();
    }
}
