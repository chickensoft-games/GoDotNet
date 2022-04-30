namespace GoDotNet {
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Collections.Specialized;
  using System.Linq;

  /// <summary>
  /// A Dictionary that preserves key insertion order.
  /// Credit: https://stackoverflow.com/a/1396743
  /// </summary>
  /// <typeparam name="TKey">Key type.</typeparam>
  /// <typeparam name="TValue">Value type.</typeparam>
  public class Map<TKey, TValue> {
    private OrderedDictionary _collection { get; } = new OrderedDictionary();

    /// <summary>Retrieve a map value by key.</summary>
    public TValue this[TKey key] {
      get => (TValue)_collection[key];
      set => _collection[key] = value;
    }

    /// <summary>Retrieve a map value by index.</summary>
    public TValue this[int index] {
      get => (TValue)_collection[index];
      set => _collection[index] = value;
    }
    /// <summary>List of keys.</summary>
    public ICollection<TKey> Keys => _collection.Keys.OfType<TKey>().ToList();
    /// <summary>List of values.</summary>
    public ICollection<TValue> Values => _collection.Values.OfType<TValue>().ToList();
    /// <summary>Whether or not the map is read-only.</summary>
    public bool IsReadOnly => _collection.IsReadOnly;
    /// <summary>Number of key/value pairs in the map.</summary>
    public int Count => _collection.Count;
    /// <summary>Map enumerator.</summary>
    public IDictionaryEnumerator GetEnumerator() => _collection.GetEnumerator();
    /// <summary>Insert a key and value at the specified index.</summary>
    public void Insert(int index, TKey key, TValue value) => _collection.Insert(index, key, value);
    /// <summary>Remove a key/value pair at the specified index.</summary>
    public void RemoveAt(int index) => _collection.RemoveAt(index);
    /// <summary>True if the given key is in the map.</summary>
    public bool Contains(TKey key) => _collection.Contains(key);
    /// <summary>Adds or updates a key/value pair to the map.</summary>
    public void Add(TKey key, TValue value) => _collection.Add(key, value);
    /// <summary>Removes all entries from the map.</summary>
    public void Clear() => _collection.Clear();
    /// <summary>Remove a key/value pair from the map.</summary>
    public void Remove(TKey key) => _collection.Remove(key);
    /// <summary>Copy the map to an array, beginning at the given index.</summary>
    public void CopyTo(Array array, int index) => _collection.CopyTo(array, index);
  }
}
