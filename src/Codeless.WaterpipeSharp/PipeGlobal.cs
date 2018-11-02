using Codeless.Ecma;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Codeless.WaterpipeSharp {
  /// <summary>
  /// Represents a collection of global values accessible regardless of the current context in the evaluation stack.
  /// </summary>
  public class PipeGlobal : IDictionary<string, EcmaValue> {
    private readonly Dictionary<string, object> dictionary = new Dictionary<string, object>();
    private readonly PipeGlobal parent;

    /// <summary>
    /// Instantiate the <see cref="PipeGlobal"/> class.
    /// </summary>
    public PipeGlobal() { }

    /// <summary>
    /// Instantiate the <see cref="PipeGlobal"/> class initialized with given entries.
    /// Dictionary keys are converted to string values through <see cref="EcmaValue"/> string conversion.
    /// If two distinct keys give the same string representation, exception will be thrown.
    /// </summary>
    /// <param name="ht"></param>
    public PipeGlobal(IDictionary ht) {
      foreach (DictionaryEntry e in ht) {
        Add(new EcmaValue(e.Key).ToString(), new EcmaValue(e.Value).GetUnderlyingObject());
      }
    }

    /// <summary>
    /// Instantiate the <see cref="PipeGlobal"/> class inheriting entries in another <see cref="PipeGlobal"/> instance.
    /// This newly created instance will expose the inherited entries but not modifying them.
    /// Setting value with the same key that appears in the parent <see cref="PipeGlobal"/>instance will mask that entry with the new value.
    /// </summary>
    /// <param name="copyFrom"></param>
    public PipeGlobal(PipeGlobal copyFrom) {
      foreach (string key in copyFrom.dictionary.Keys) {
        this[key] = copyFrom[key];
      }
    }

    /// <summary>
    /// Gets or sets value associated with the given key.
    /// If the given key is not present in the dictionary, <see cref="EcmaValue.Undefined"/> is returned.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public EcmaValue this[string key] {
      get {
        object value;
        if (dictionary.TryGetValue(key, out value)) {
          return new EcmaValue(value);
        }
        if (parent != null) {
          return parent[key];
        }
        return EcmaValue.Undefined;
      }
      set {
        dictionary[key] = value.GetUnderlyingObject();
      }
    }

    /// <summary>
    /// Gets the number of entries contained including inherited entries if any.
    /// </summary>
    public int Count {
      get { return dictionary.Count + (parent != null ? parent.Count : 0); }
    }

    public void Add(string key, object value) {
      dictionary.Add(key, value);
    }

    public void Add(string key, EcmaValue value) {
      dictionary.Add(key, value.GetUnderlyingObject());
    }

    public void Clear() {
      dictionary.Clear();
    }

    public bool ContainsKey(string key) {
      return dictionary.ContainsKey(key) || (parent != null && parent.ContainsKey(key));
    }

    public bool Remove(string key) {
      return dictionary.Remove(key);
    }

    public bool TryGetValue(string key, out EcmaValue value) {
      object value1;
      if (dictionary.TryGetValue(key, out value1)) {
        value = new EcmaValue(value1);
        return true;
      }
      if (parent != null) {
        return parent.TryGetValue(key, out value);
      }
      value = default(EcmaValue);
      return false;
    }

    #region Interfaces
    bool ICollection<KeyValuePair<string, EcmaValue>>.IsReadOnly {
      get { return false; }
    }

    ICollection<string> IDictionary<string, EcmaValue>.Keys {
      get { return dictionary.Keys; }
    }

    ICollection<EcmaValue> IDictionary<string, EcmaValue>.Values {
      get { return dictionary.Values.Select(v => new EcmaValue(v)).ToArray(); }
    }

    void ICollection<KeyValuePair<string, EcmaValue>>.Add(KeyValuePair<string, EcmaValue> item) {
      dictionary.Add(item.Key, item.Value);
    }

    bool ICollection<KeyValuePair<string, EcmaValue>>.Contains(KeyValuePair<string, EcmaValue> item) {
      return false;// return dictionary.Contains(item);
    }

    bool ICollection<KeyValuePair<string, EcmaValue>>.Remove(KeyValuePair<string, EcmaValue> item) {
      return dictionary.Remove(item.Key);
    }

    void ICollection<KeyValuePair<string, EcmaValue>>.CopyTo(KeyValuePair<string, EcmaValue>[] array, int arrayIndex) {
      throw new NotImplementedException();
    }

    IEnumerator<KeyValuePair<string, EcmaValue>> IEnumerable<KeyValuePair<string, EcmaValue>>.GetEnumerator() {
      return dictionary.Select(v => new KeyValuePair<string, EcmaValue>(v.Key, new EcmaValue(v.Value))).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return dictionary.GetEnumerator();
    }
    #endregion
  }
}
