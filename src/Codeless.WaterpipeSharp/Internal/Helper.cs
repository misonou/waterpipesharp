using Codeless.Ecma;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Codeless.WaterpipeSharp.Internal {
  internal class Helper {
    public static bool Evallable(EcmaValue value) {
      return !value.IsNullOrUndefined;
    }

    public static string Json(EcmaValue value) {
      try {
        return Ecma.Json.Stringify(value);
      } catch {
        return String(value);
      }
    }

    public static string String(EcmaValue value, Func<EcmaValue, string> stringify = null) {
      return !Evallable(value) || value.IsNaN || value.TypeOf == "function" ? "" : value.Type == EcmaValueType.String || stringify == null ? (string)value : stringify(value);
    }

    public static PipeLambda DetectKeyLambda(PipeContext context, EcmaValue arr) {
      switch (context.State) {
        case PipeState.Func:
          return context.TakeLambda();
        case PipeState.Auto:
          var key = context.TakeArgumentAsRaw();
          var value = context.Evaluate((string)key);
          if (value.Type == EcmaValueType.String && IsValidKey(context, arr, value)) {
            return PipeLambdaFactory.PropertyAccess.CreateLambda(value);
          }
          var fn = context.EvaluationContext.ResolveFunction((string)key);
          if (fn != null && !fn.IsVariadic && !IsValidKey(context, arr, key)) {
            return (c, a, b) => fn.Invoke(c, new[] { a, b });
          }
          return PipeLambdaFactory.PropertyAccess.CreateLambda(key);
      }
      return PipeLambdaFactory.PropertyAccess.CreateLambda(String(context.TakeArgument()));
    }

    private static bool IsValidKey(PipeContext context, EcmaValue arr, EcmaValue value) {
      return (bool)arr.First(context, (c, a, b) => HasProperty(a, EcmaPropertyKey.FromValue(value)), true);
    }

    public static bool HasProperty(EcmaValue value, EcmaPropertyKey name) {
      if (value.IsNullOrUndefined || value[name].IsCallable) {
        return false;
      }
      if (value.Type != EcmaValueType.Object) {
        return value[name].Type != EcmaValueType.Undefined;
      }
      return value.HasOwnProperty(name) || (EcmaArray.IsArray(value) && Int32.TryParse(name.Name, out int _));
    }

    private static readonly Regex reEscape = new Regex("[\"'&<>]");
    private static readonly Regex reEscapeNoEntity = new Regex("[\"'<>]|&(?!#\\d+;|[a-z][a-z0-9]+;)", RegexOptions.IgnoreCase);

    public static string Escape(string str, bool preserveEntity) {
      return (preserveEntity ? reEscapeNoEntity : reEscape).Replace(str, m => {
        switch (m.Value[0]) {
          case '"': return "&quot;";
          case '&': return "&amp;";
          case '\'': return "&#39;";
          case '<': return "&lt;";
          case '>': return "&gt;";
        }
        return m.Value;
      });
    }
  }
}
