using Codeless.Ecma;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Codeless.WaterpipeSharp {
  public delegate EcmaValue PipeLambda(EcmaValue obj, EcmaValue index);

  public static class PipeLambdaExtension {
    public static EcmaValue Invoke(this PipeLambda fn, EcmaValue value) {
      return fn(value, EcmaValue.Undefined);
    }

    public static EcmaValue Invoke(this PipeLambda fn, EcmaPropertyEntry entry) {
      return fn(entry.Value, entry.Key.ToString());
    }
  }
}
