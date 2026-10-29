using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Lab1;

/// <summary>
/// A thread-safe queue with a size limit, built only from what the lectures showed:
/// ConcurrentQueue (Lecture 3) for the items and SemaphoreSlim (Lecture 2: "semaphores answer HOW MANY")
/// for the three rules of the producer-consumer picture:
///   queue full   -> the producer waits    (_free has no permits)
///   queue empty  -> the consumers wait    (_filled has no permits)
///   Close        -> consumers finish what is left and stop
/// </summary>
public sealed class BoundedQueue<T>
{
    readonly ConcurrentQueue<T> _items = new();
    readonly SemaphoreSlim _free;                 // how many free places are left
    readonly SemaphoreSlim _filled = new(0);      // how many items are waiting
    readonly int _consumers;

    public BoundedQueue(int capacity, int consumers)
    {
        _free = new SemaphoreSlim(capacity);
        _consumers = consumers;
    }

    /// <summary>Producer: waits while the queue is full.</summary>
    public void Add(T item)
    {
        _free.Wait();
        _items.Enqueue(item);
        _filled.Release();                        // enqueue FIRST, release after: a woken consumer finds the item
    }

    /// <summary>
    /// Producer: no more items. One extra permit per consumer wakes everybody who is waiting on an empty queue.
    /// Permits = items left + consumers, so every consumer gets exactly one "empty and closed" answer.
    /// </summary>
    public void Close() => _filled.Release(_consumers);

    /// <summary>Consumer: waits while the queue is empty. Returns false when it is closed and empty.</summary>
    public bool TryTake([MaybeNullWhen(false)] out T item)
    {
        _filled.Wait();
        if (_items.TryDequeue(out item))
        {
            _free.Release();
            return true;
        }
        return false;
    }
}
