using Codeless.Ecma;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Codeless.WaterpipeSharp.Internal {
  internal class EvaluationStack {
    private readonly EcmaValue value;
    private IEnumerator<EcmaPropertyEntry> enumerator;
    private int index = -1;
    private int? count;

    public EvaluationStack(EcmaValue value) {
      this.value = value;
    }

    public EcmaValue Value {
      get { return enumerator != null ? enumerator.Current.Value : value; }
    }

    public EcmaValue Index {
      get { return enumerator != null ? index : EcmaValue.Undefined; }
    }

    public EcmaValue Key {
      get { return enumerator != null ? enumerator.Current.Key.ToString() : EcmaValue.Undefined; }
    }

    public int Count {
      get {
        if (count == null) {
          count = value.EnumerateKeys().Count();
        }
        return count.Value;
      }
    }

    public bool MoveNext() {
      if (enumerator == null) {
        enumerator = value.EnumerateEntries().GetEnumerator();
      }
      if (enumerator.MoveNext()) {
        index++;
        return true;
      }
      return false;
    }
  }
}
