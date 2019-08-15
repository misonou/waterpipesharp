using Codeless.Ecma;
using Codeless.Ecma.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Codeless.WaterpipeSharp {
  public class PipeValueComparer : Comparer<object> {
    public static new readonly PipeValueComparer Default = new PipeValueComparer();

    public override int Compare(object x_, object y_) {
      EcmaValue x = new EcmaValue(x_);
      EcmaValue y = new EcmaValue(y_);
      if (x.IsArrayLike && y.IsArrayLike) {
        int result = 0;
        IEnumerator<EcmaPropertyKey> iterX = x.ToObject().GetEnumerablePropertyKeys().GetEnumerator();
        IEnumerator<EcmaPropertyKey> iterY = y.ToObject().GetEnumerablePropertyKeys().GetEnumerator();
        while (iterX.MoveNext() && iterY.MoveNext() && result == 0) {
          result = Compare(x[iterX.Current], y[iterY.Current]);
        }
        return result != 0 ? result : (int)x["length"] - (int)y["length"];
      }
      return x == y ? 0 : x.IsNullOrUndefined || x < y ? -1 : y.IsNullOrUndefined || x > y ? 1 : 0;
    }
  }
}
