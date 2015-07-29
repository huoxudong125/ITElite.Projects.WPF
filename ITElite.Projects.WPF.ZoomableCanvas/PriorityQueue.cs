//http://blogs.msdn.com/b/kaelr/archive/2006/01/09/priorityqueue.aspx

using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic
{
    /// <summary>
    ///     Represents a queue of items that are sorted based on individual priorities.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the queue.</typeparam>
    /// <typeparam name="TPriority">Specifies the type of object representing the priority.</typeparam>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public class PriorityQueue<T, TPriority>
    {
        private readonly IComparer<TPriority> comparer;
        private readonly List<KeyValuePair<T, TPriority>> heap = new List<KeyValuePair<T, TPriority>>();
        private readonly Dictionary<T, int> indexes = new Dictionary<T, int>();
        private readonly bool invert;

        public PriorityQueue()
            : this(false)
        {
        }

        public PriorityQueue(bool invert)
            : this(Comparer<TPriority>.Default)
        {
            this.invert = invert;
        }

        public PriorityQueue(IComparer<TPriority> comparer)
        {
            this.comparer = comparer;
            heap.Add(default(KeyValuePair<T, TPriority>));
        }

        public TPriority this[T item]
        {
            get { return heap[indexes[item]].Value; }
            set
            {
                int index;

                if (indexes.TryGetValue(item, out index))
                {
                    var order = comparer.Compare(value, heap[index].Value);
                    if (order != 0)
                    {
                        if (invert)
                            order = ~order;

                        var element = new KeyValuePair<T, TPriority>(item, value);
                        if (order < 0)
                            MoveUp(element, index);
                        else
                            MoveDown(element, index);
                    }
                }
                else
                {
                    var element = new KeyValuePair<T, TPriority>(item, value);
                    heap.Add(element);

                    MoveUp(element, Count);
                }
            }
        }

        public int Count
        {
            get { return heap.Count - 1; }
        }

        public void Enqueue(T item, TPriority priority)
        {
            var tail = new KeyValuePair<T, TPriority>(item, priority);
            heap.Add(tail);

            MoveUp(tail, Count);
        }

        public KeyValuePair<T, TPriority> Dequeue()
        {
            var bound = Count;
            if (bound < 1)
                throw new InvalidOperationException("Queue is empty.");

            var head = heap[1];
            var tail = heap[bound];

            heap.RemoveAt(bound);

            if (bound > 1)
                MoveDown(tail, 1);

            indexes.Remove(head.Key);

            return head;
        }

        public KeyValuePair<T, TPriority> Peek()
        {
            if (Count < 1)
                throw new InvalidOperationException("Queue is empty.");

            return heap[1];
        }

        public bool TryGetValue(T item, out TPriority priority)
        {
            int index;
            if (indexes.TryGetValue(item, out index))
            {
                priority = heap[indexes[item]].Value;
                return true;
            }
            priority = default(TPriority);
            return false;
        }

        private void MoveUp(KeyValuePair<T, TPriority> element, int index)
        {
            while (index > 1)
            {
                var parent = index >> 1;

                if (IsPrior(heap[parent], element))
                    break;

                heap[index] = heap[parent];
                indexes[heap[parent].Key] = index;

                index = parent;
            }

            heap[index] = element;
            indexes[element.Key] = index;
        }

        private void MoveDown(KeyValuePair<T, TPriority> element, int index)
        {
            var count = heap.Count;

            while (index << 1 < count)
            {
                var child = index << 1;
                var sibling = child | 1;

                if (sibling < count && IsPrior(heap[sibling], heap[child]))
                    child = sibling;

                if (IsPrior(element, heap[child]))
                    break;

                heap[index] = heap[child];
                indexes[heap[child].Key] = index;

                index = child;
            }

            heap[index] = element;
            indexes[element.Key] = index;
        }

        private bool IsPrior(KeyValuePair<T, TPriority> element1, KeyValuePair<T, TPriority> element2)
        {
            var order = comparer.Compare(element1.Value, element2.Value);
            if (invert)
                order = ~order;
            return order < 0;
        }
    }
}