using Codeless.Ecma;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Codeless.WaterpipeSharp {
  public static class PipeValueExtension {
    public static EcmaValue Where(this EcmaValue value, PipeLambda fn) {
      Guard.ArgumentNotNull(fn, "fn");
      PipeValueObjectBuilder collection = new PipeValueObjectBuilder(value.IsArrayLike);
      foreach (EcmaPropertyEntry item in value.EnumerateEntries()) {
        if ((bool)fn.Invoke(item)) {
          collection.Add(item.Value, item.Key.ToString());
        }
      }
      return collection;
    }

    public static EcmaValue Map(this EcmaValue value, PipeLambda fn) {
      Guard.ArgumentNotNull(fn, "fn");
      PipeValueObjectBuilder collection = new PipeValueObjectBuilder(value.IsArrayLike);
      foreach (EcmaPropertyEntry item in value.EnumerateEntries()) {
        collection.Add(fn.Invoke(item), item.Key.ToString());
      }
      return collection;
    }

    public static EcmaValue First(this EcmaValue value, PipeLambda fn) {
      return First(value, fn, false, false);
    }

    public static EcmaValue First(this EcmaValue value, PipeLambda fn, bool returnBoolean) {
      return First(value, fn, returnBoolean, false);
    }

    public static EcmaValue First(this EcmaValue value, PipeLambda fn, bool returnBoolean, bool negate) {
      Guard.ArgumentNotNull(fn, "fn");
      foreach (EcmaPropertyEntry item in value.EnumerateEntries()) {
        if (negate ^ (bool)fn.Invoke(item)) {
          return returnBoolean ? true : item.Value;
        }
      }
      return returnBoolean ? false : EcmaValue.Undefined;
    }

    public static EcmaValue Flatten(this EcmaValue src) {
      if (src.IsArrayLike) {
        PipeValueObjectBuilder dst = new PipeValueObjectBuilder(true);
        foreach (EcmaValue value in src.EnumerateValues()) {
          if (value.IsArrayLike) {
            EcmaValue fvalue = Flatten(value);
            foreach (EcmaValue o in fvalue.EnumerateValues()) {
              if (o != EcmaValue.Null && o != EcmaValue.Undefined)
                dst.Add(o, "");
            }
          } else if (value != EcmaValue.Null && value != EcmaValue.Undefined) {
            dst.Add(value, "");
          }
        }
        return dst.ToPipeValue();
      }
      return src;
    }

    public static EcmaValue[] ToArray(this EcmaValue arr) {
      return arr.IsArrayLike ? arr.EnumerateValues().ToArray() : new[] { arr };
    }
  }
}
