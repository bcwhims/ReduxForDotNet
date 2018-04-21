using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;

namespace ReduxForDotNet
{
    public class ObservableQueue<T> : IProducerConsumerCollection<T>, IEnumerable<T>, IEnumerable, ICollection, IReadOnlyCollection<T>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private ConcurrentQueue<T> queue;

         public ObservableQueue()
        {
            queue = new ConcurrentQueue<T>();
        }

        public int Count => queue.Count;
        bool ICollection.IsSynchronized => ((ICollection)queue).IsSynchronized;
        public object SyncRoot => throw new NotImplementedException();
        public bool IsEmpty => queue.IsEmpty;


        public void Enqueue(T item)
        {
            queue.Enqueue(item);
            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public bool TryDequeue(out T result)
        {
            var isSuccessful = queue.TryDequeue(out result);
            if (isSuccessful)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, result));
            }

            return isSuccessful;
        }

        public bool TryPeek(out T result)
        {
            return queue.TryPeek(out result);
        }

        public void CopyTo(T[] array, int index)
        {
            queue.CopyTo(array, index);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)queue).CopyTo(array, index);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return queue.GetEnumerator();
        }

        public T[] ToArray()
        {
            return queue.ToArray();
        }

        bool IProducerConsumerCollection<T>.TryAdd(T item)
        {
            var isSuccessful = ((IProducerConsumerCollection<T>)queue).TryAdd(item);
            if (isSuccessful)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
            }

            return isSuccessful;
        }

        bool IProducerConsumerCollection<T>.TryTake(out T item)
        {
            var isSuccessful = ((IProducerConsumerCollection<T>)queue).TryTake(out item);
            if (isSuccessful)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove));
            }

            return isSuccessful;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)queue).GetEnumerator();
        }
    }
}
