using Codeless.Ecma;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Codeless.WaterpipeSharp {
  public class PipeValueObjectBuilder {
    private readonly List<object> array = new List<object>();
    private readonly bool isArray;

    public PipeValueObjectBuilder(bool isArray) {
      this.isArray = isArray;
    }

    public PipeValueObjectBuilder(IEnumerable values)
      : this(true) {
      Guard.ArgumentNotNull(values, "values");
      foreach (object value in values) {
        array.Add(new EcmaValue(value).GetUnderlyingObject());
      }
    }

    public PipeValueObjectBuilder(IEnumerable<EcmaValue> values)
      : this(true) {
      Guard.ArgumentNotNull(values, "values");
      foreach (EcmaValue value in values) {
        array.Add(value.GetUnderlyingObject());
      }
    }

    public PipeValueObjectBuilder(IDictionary values)
      : this(false) {
      Guard.ArgumentNotNull(values, "values");
      foreach (string key in values.Keys.OfType<string>()) {
        array.Add(new KeyValuePair<string, object>(key, new EcmaValue(values[key]).GetUnderlyingObject()));
      }
    }

    public int Count {
      get { return array.Count; }
    }

    public void Add(EcmaValue value, string key) {
      if (isArray) {
        array.Add(value.GetUnderlyingObject());
      } else {
        array.Add(new KeyValuePair<string, object>(key, value.GetUnderlyingObject()));
      }
    }

    public EcmaValue ToPipeValue() {
      if (isArray) {
        return new EcmaValue(array.ToArray());
      }
      ICollection<KeyValuePair<string, object>> dictionary = new Dictionary<string, object>();
      foreach (KeyValuePair<string, object> e in array) {
        dictionary.Add(e);
      }
      return new EcmaValue(dictionary);
    }

    public static implicit operator EcmaValue(PipeValueObjectBuilder collection) {
      return collection.ToPipeValue();
    }
  }
}
