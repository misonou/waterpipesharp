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
    public static EcmaValue Where(this EcmaValue value, PipeContext context, PipeLambda fn) {
      Guard.ArgumentNotNull(context, "context");
      Guard.ArgumentNotNull(fn, "fn");
      PipeValueObjectBuilder collection = new PipeValueObjectBuilder(value.IsArrayLike);
      foreach (EcmaPropertyKey propertyKey in value) {
        if ((bool)fn.Invoke(context, value[propertyKey], propertyKey.ToValue())) {
          collection.Add(value[propertyKey], propertyKey.ToString());
        }
      }
      return collection;
    }

    public static EcmaValue Map(this EcmaValue value, PipeContext context, PipeLambda fn) {
      Guard.ArgumentNotNull(context, "context");
      Guard.ArgumentNotNull(fn, "fn");
      PipeValueObjectBuilder collection = new PipeValueObjectBuilder(value.IsArrayLike);
      foreach (EcmaPropertyKey propertyKey in value) {
        collection.Add(fn.Invoke(context, value[propertyKey], propertyKey.ToValue()), propertyKey.ToString());
      }
      return collection;
    }

    public static EcmaValue First(this EcmaValue value, PipeContext context, PipeLambda fn) {
      return First(value, context, fn, false, false);
    }

    public static EcmaValue First(this EcmaValue value, PipeContext context, PipeLambda fn, bool returnBoolean) {
      return First(value, context, fn, returnBoolean, false);
    }

    public static EcmaValue First(this EcmaValue value, PipeContext context, PipeLambda fn, bool returnBoolean, bool negate) {
      Guard.ArgumentNotNull(context, "context");
      Guard.ArgumentNotNull(fn, "fn");
      foreach (EcmaPropertyKey propertyKey in value) {
        if (negate ^ (bool)fn.Invoke(context, value[propertyKey], propertyKey.ToValue())) {
          return returnBoolean ? true : value[propertyKey];
        }
      }
      return returnBoolean ? false : EcmaValue.Undefined;
    }

    public static EcmaValue Flatten(this EcmaValue src) {
      if (src.IsArrayLike) {
        PipeValueObjectBuilder dst = new PipeValueObjectBuilder(true);
        foreach (EcmaPropertyKey propertyKey in src) {
          EcmaValue value = src[propertyKey];
          if (value.IsArrayLike) {
            EcmaValue fvalue = Flatten(value);
            foreach (EcmaPropertyKey propertyKey2 in fvalue) {
              EcmaValue o = fvalue[propertyKey2];
              if (o != EcmaValue.Null && o != EcmaValue.Undefined) {
                dst.Add(o, "");
              }
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
      return arr.IsArrayLike ? EcmaValueUtility.CreateListFromArrayLike(arr) : new[] { arr };
    }
  }
}
