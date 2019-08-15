using Codeless.Ecma;
using Codeless.Ecma.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Codeless.WaterpipeSharp.Internal {
  internal class EvaluationStack {
    private readonly EcmaValue value;
    private IEnumerator<EcmaPropertyKey> enumerator;
    private int index = -1;
    private int? count;

    public EvaluationStack(EcmaValue value) {
      this.value = value;
    }

    public EcmaValue Value {
      get { return enumerator != null ? value[enumerator.Current] : value; }
    }

    public EcmaValue Index {
      get { return enumerator != null ? index : EcmaValue.Undefined; }
    }

    public EcmaValue Key {
      get { return enumerator != null ? enumerator.Current.ToString() : EcmaValue.Undefined; }
    }

    public int Count {
      get {
        if (count == null) {
          count = value.IsNullOrUndefined ? 0 : value.ToObject().GetEnumerablePropertyKeys().Count();
        }
        return count.Value;
      }
    }

    public bool MoveNext() {
      if (enumerator == null && !value.IsNullOrUndefined) {
        enumerator = value.ToObject().GetEnumerablePropertyKeys().GetEnumerator();
      }
      if (enumerator != null && enumerator.MoveNext()) {
        index++;
        return true;
      }
      return false;
    }
  }
}
