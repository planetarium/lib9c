using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model
{
    /// <summary>
    /// Represents a collection map with key-value pairs.
    /// Supports both Dictionary and List serialization formats for backward compatibility.
    ///
    /// <para>
    /// Serialization Format:
    /// - Dictionary (Legacy): Uses key-value pairs for backward compatibility
    /// - List (New): Uses ordered list for better performance and smaller size
    /// </para>
    ///
    /// <para>
    /// Field Order (List Format):
    /// 1. version - Serialization version number
    /// 2. entries - List of key-value pairs ordered by key
    /// </para>
    /// </summary>
    /// <remarks>
    /// This class implements dual serialization support to ensure smooth migration
    /// from the legacy Dictionary format to the new List format. The List format
    /// provides better performance and smaller serialized data size.
    ///
    /// <para>
    /// Example usage:
    /// <code>
    /// // Create collection map
    /// var map = new CollectionMap();
    /// map.Add(1, 100);
    /// map.Add(2, 200);
    ///
    /// // Serialize to List format (new)
    /// var serialized = map.Serialize(); // Returns List
    ///
    /// // Deserialize from any format
    /// var deserialized = new CollectionMap(serialized); // Supports both Dictionary and List
    /// </code>
    /// </para>
    /// </remarks>
    [Serializable]
    public class CollectionMap : IState, IDictionary<int, int>, ISerializable
    {
        // Serialization version for backward compatibility
        private const int SerializationVersion = 1;

        // Field count constants for serialization
        private const int CollectionMapFieldCount = 2; // version + entries

        private Dictionary<int, int> _dictionary;
        private IValue _serialized;

        public CollectionMap()
        {
            _dictionary = new Dictionary<int, int>();
        }

        /// <summary>
        /// Constructor for deserialization that supports both Dictionary and List formats.
        /// </summary>
        /// <param name="serialized">Serialized data in either Dictionary or List format</param>
        /// <exception cref="ArgumentNullException">Thrown when serialized is null</exception>
        /// <exception cref="ArgumentException">Thrown when serialized format is not supported</exception>
        public CollectionMap(IValue serialized)
        {
            if (serialized == null)
            {
                throw new ArgumentNullException(nameof(serialized), "Serialized data cannot be null");
            }
            _serialized = serialized;

            switch (serialized)
            {
                case Dictionary dict:
                    DeserializeFromDictionary(dict);
                    break;
                case List list:
                    DeserializeFromList(list);
                    break;
                default:
                    throw new ArgumentException(
                        $"Unsupported serialization format: {serialized.GetType().Name}. " +
                        $"Expected Dictionary or List, got {serialized.GetType().Name}. " +
                        $"This may indicate corrupted data or an unsupported serialization format.");
            }
        }

        /// <summary>
        /// Constructor for backward compatibility with Dictionary format.
        /// </summary>
        /// <param name="serialized">Dictionary containing serialized data</param>
        public CollectionMap(Bencodex.Types.Dictionary serialized)
        {
            DeserializeFromDictionary(serialized);
        }

        /// <summary>
        /// Deserializes data from Dictionary format (legacy support).
        /// </summary>
        /// <param name="dict">Dictionary containing serialized data</param>
        private void DeserializeFromDictionary(Dictionary dict)
        {
            _dictionary = dict.ToDictionary(
                kv => kv.Key.ToInteger(),
                kv => kv.Value.ToInteger()
            );
        }

        /// <summary>
        /// Deserializes data from List format (new format).
        /// Order: [version, entries]
        /// </summary>
        /// <param name="list">List containing serialized data</param>
        private void DeserializeFromList(List list)
        {
            // Check if we have enough fields for CollectionMap
            if (list.Count < CollectionMapFieldCount)
            {
                var fieldNames = string.Join(", ", GetFieldNames());
                throw new ArgumentException(
                    $"Invalid list length for {GetType().Name}: expected at least {CollectionMapFieldCount}, got {list.Count}. " +
                    $"Required fields: {fieldNames}. " +
                    $"This may indicate corrupted data or an unsupported serialization format.");
            }

            // version (index 0)
            var version = ((Integer)list[0]).Value;
            if (version != SerializationVersion)
            {
                throw new ArgumentException(
                    $"Unsupported serialization version: {version}. " +
                    $"Expected {SerializationVersion}. " +
                    $"This may indicate corrupted data or an unsupported serialization format.");
            }

            // entries (index 1)
            var entriesList = (List)list[1];
            _dictionary = new Dictionary<int, int>();

            foreach (var entry in entriesList)
            {
                var entryList = (List)entry;
                var key = entryList[0].ToInteger();
                var value = entryList[1].ToInteger();
                _dictionary[key] = value;
            }
        }

        private CollectionMap(SerializationInfo info, StreamingContext context)
        {
            _dictionary = (Dictionary<int, int>)info.GetValue(
                nameof(Dictionary),
                typeof(Dictionary<int, int>)
            );
        }

        private Dictionary<int, int> Dictionary
        {
            get
            {
                if (_serialized is Dictionary d)
                {
                    _dictionary = d.ToDictionary(
                        kv => kv.Key.ToInteger(),
                        kv => kv.Value.ToInteger()
                    );
                    _serialized = null;
                    return _dictionary;
                }

                if (_serialized is List l)
                {
                    DeserializeFromList(l);
                    _serialized = null;
                }

                return _dictionary;
            }

            set
            {
                _dictionary = value;
                _serialized = null;
            }
        }

        /// <summary>
        /// Serializes the CollectionMap to List format (new format).
        /// Order: [version, entries]
        /// </summary>
        /// <returns>List containing serialized data</returns>
        public IValue Serialize()
        {
            var entries = Dictionary
                .OrderBy(kv => kv.Key)
                .Select(kv => List.Empty
                    .Add(kv.Key.Serialize())
                    .Add(kv.Value.Serialize()));

            return List.Empty
                .Add(SerializationVersion)
                .Add(new List(entries));
        }

        /// <summary>
        /// Gets the field names for serialization in order.
        /// </summary>
        /// <returns>Array of field names</returns>
        private static string[] GetFieldNames()
        {
            return new[] { "version", "entries" };
        }

        public void Add(KeyValuePair<int, int> item)
        {
            if (Dictionary.ContainsKey(item.Key))
            {
                Dictionary[item.Key] += item.Value;
            }
            else
            {
                Dictionary[item.Key] = item.Value;
            }
        }

        public void Clear()
        {
            Dictionary.Clear();
        }

        public bool Contains(KeyValuePair<int, int> item)
        {
#pragma warning disable LAA1002
            return Dictionary.Contains(item);
#pragma warning restore LAA1002
        }

        public void CopyTo(KeyValuePair<int, int>[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(KeyValuePair<int, int> item)
        {
            return Dictionary.Remove(item.Key);
        }

        public int Count => Dictionary.Count;
        public bool IsReadOnly => false;

        public IEnumerator<KeyValuePair<int, int>> GetEnumerator()
        {
            return Dictionary.OrderBy(kv => kv.Key).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(int key, int value)
        {
            Add(new KeyValuePair<int, int>(key, value));
        }

        public bool ContainsKey(int key)
        {
            return Dictionary.ContainsKey(key);
        }

        public bool Remove(int key)
        {
            return Dictionary.Remove(key);
        }

        public bool TryGetValue(int key, out int value)
        {
            return Dictionary.TryGetValue(key, out value);
        }

        public int this[int key]
        {
            get => Dictionary[key];
            set => Dictionary[key] = value;
        }

        public ICollection<int> Keys => Dictionary.Keys;
        public ICollection<int> Values => Dictionary.Values;

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Dictionary), Dictionary);
        }
    }
}
