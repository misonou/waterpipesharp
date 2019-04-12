using Codeless.Ecma;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Codeless.WaterpipeSharp {
  public delegate EcmaValue PipeLambda(PipeContext context, EcmaValue obj, EcmaValue index);

  public static class PipeLambdaExtension {
    public static EcmaValue Invoke(this PipeLambda fn, PipeContext context, EcmaValue value) {
      return fn(context, value, EcmaValue.Undefined);
    }

    public static EcmaValue Invoke(this PipeLambda fn, PipeContext context, EcmaPropertyEntry entry) {
      return fn(context, entry.Value, entry.Key.ToString());
    }
  }
}
