using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Distributed.Internal.Util
{
    public class ObservableDictionary<TKey, TValue> :
        ICollection<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>,
        INotifyCollectionChanged, INotifyPropertyChanged
    {
        readonly IDictionary<TKey, TValue> dictionary;

        public event NotifyCollectionChangedEventHandler CollectionChanged = (sender, args) => { };
        public event PropertyChangedEventHandler PropertyChanged = (sender, args) => { };

        public ObservableDictionary() : this(new Dictionary<TKey, TValue>()) { }
        public ObservableDictionary(IDictionary<TKey, TValue> dictionary) => this.dictionary = dictionary;
        void AddWithNotification(KeyValuePair<TKey, TValue> item) => AddWithNotification(item.Key, item.Value);

        void AddWithNotification(TKey key, TValue value)
        {
            dictionary.Add(key, value);

            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value)));
            PropertyChanged(this, new PropertyChangedEventArgs("Count"));
            PropertyChanged(this, new PropertyChangedEventArgs("Keys"));
            PropertyChanged(this, new PropertyChangedEventArgs("Values"));
        }

        bool RemoveWithNotification(TKey key)
        {
            if (dictionary.TryGetValue(key, out var value) && dictionary.Remove(key))
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>(key, value)));
                PropertyChanged(this, new PropertyChangedEventArgs("Count"));
                PropertyChanged(this, new PropertyChangedEventArgs("Keys"));
                PropertyChanged(this, new PropertyChangedEventArgs("Values"));

                return true;
            }

            return false;
        }

        void UpdateWithNotification(TKey key, TValue value)
        {
            if (dictionary.TryGetValue(key, out var existing))
            {
                dictionary[key] = value;

                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                    new KeyValuePair<TKey, TValue>(key, value),
                    new KeyValuePair<TKey, TValue>(key, existing)));
                PropertyChanged(this, new PropertyChangedEventArgs("Values"));
            }
            else
            {
                AddWithNotification(key, value);
            }
        }

        protected void RaisePropertyChanged(PropertyChangedEventArgs args) => PropertyChanged(this, args);
        public void Add(TKey key, TValue value) => AddWithNotification(key, value);
        public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);
        public ICollection<TKey> Keys => dictionary.Keys;
        public bool Remove(TKey key) => RemoveWithNotification(key);
        public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);
        public ICollection<TValue> Values => dictionary.Values;

        public TValue this[TKey key]
        {
            get => dictionary[key];
            set => UpdateWithNotification(key, value);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => AddWithNotification(item);
        public void Clear()
        {
            dictionary.Clear();

            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            PropertyChanged(this, new PropertyChangedEventArgs("Count"));
            PropertyChanged(this, new PropertyChangedEventArgs("Keys"));
            PropertyChanged(this, new PropertyChangedEventArgs("Values"));
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Contains(item);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => dictionary.CopyTo(array, arrayIndex);
        int ICollection<KeyValuePair<TKey, TValue>>.Count => dictionary.Count;
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => dictionary.IsReadOnly;
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => RemoveWithNotification(item.Key);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => dictionary.GetEnumerator();
        public IEnumerator GetEnumerator() => dictionary.GetEnumerator();
    }
}
