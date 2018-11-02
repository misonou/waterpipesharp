using Codeless.Ecma;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Codeless.WaterpipeSharp.Internal {
  internal enum ObjectPathEvaluateMode {
    Default = 0,
    Stack,
    IterationKey,
    IterationIndex,
    IterationCount,
    Global
  }

  internal interface IObjectPath {
    string Value { get; }
    EcmaValue Evaluate(EvaluationContext context);
  }

  [DebuggerDisplay("{Value,nq}")]
  internal class ObjectPath : Collection<IObjectPath>, IObjectPath {
    private static readonly Regex re = new Regex(@"((?!^)\$)?([^$.()][^.]*)|\$\(([^)]+)\)");
    private static readonly Regex reAt = new Regex(@"^@(\d+)$");

    public static readonly ObjectPath Empty = new ObjectPath("");

    protected ObjectPath(string str) {
      Guard.ArgumentNotNull(str, "str");
      this.Value = str;
    }

    public string Value { get; private set; }
    public int StackIndex { get; private set; }
    public ObjectPathEvaluateMode EvaluateMode { get; private set; }

    public static ObjectPath FromString(string str) {
      ObjectPath path = new ObjectPath(str);
      for (Match m = re.Match(str); m.Success; m = m.NextMatch()) {
        path.Add(m.Groups[1].Success || m.Groups[3].Success ?
           (IObjectPath)FromString(m.Groups[3].Success ? m.Groups[3].Value : m.Groups[2].Value) :
           new ConstantObjectPath(m.Groups[2].Value));
      }
      if (path.Count == 0) {
        path.Add(new ConstantObjectPath(str));
      }
      if (path[0] is ConstantObjectPath) {
        switch (path[0].Value) {
          case ".":
          case "@0":
            path.EvaluateMode = ObjectPathEvaluateMode.Stack;
            break;
          case "#":
          case "#key":
            path.EvaluateMode = ObjectPathEvaluateMode.IterationKey;
            break;
          case "##":
          case "#index":
            path.EvaluateMode = ObjectPathEvaluateMode.IterationIndex;
            break;
          case "#count":
            path.EvaluateMode = ObjectPathEvaluateMode.IterationCount;
            break;
          case "_":
          case "@root":
            path.EvaluateMode = ObjectPathEvaluateMode.Stack;
            path.StackIndex = -1;
            break;
          case "@global":
            path.EvaluateMode = ObjectPathEvaluateMode.Global;
            break;
          default:
            Match m = reAt.Match(path[0].Value);
            if (m.Success) {
              path.EvaluateMode = ObjectPathEvaluateMode.Stack;
              path.StackIndex = Int32.Parse(m.Groups[1].Value);
            }
            break;
        }
      }
      return path;
    }

    public EcmaValue Evaluate(EvaluationContext context) {
      EcmaValue value;
      TryEvaluate(context, false, out value);
      return value;
    }

    public bool TryEvaluate(EvaluationContext context, bool acceptShorthand, out EcmaValue value) {
      Guard.ArgumentNotNull(context, "context");
      if (this.Count == 0) {
        value = context.ValueAt(0);
        return true;
      }
      value = EcmaValue.Undefined;
      bool valid = acceptShorthand || this.EvaluateMode == ObjectPathEvaluateMode.Default || (this[0].Value.Length > 1 && this[0].Value != "##");
      switch (this.EvaluateMode) {
        case ObjectPathEvaluateMode.IterationKey:
          value = context.Current.Key;
          return valid;
        case ObjectPathEvaluateMode.IterationIndex:
          value = context.Current.Index;
          return valid;
        case ObjectPathEvaluateMode.IterationCount:
          value = context.Current.Count;
          return valid;
        case ObjectPathEvaluateMode.Stack:
          value = context.ValueAt(this.StackIndex < 0 ? context.StackCount + this.StackIndex : this.StackIndex);
          break;
        case ObjectPathEvaluateMode.Global:
          value = new EcmaValue(context.Globals);
          break;
        default:
          string name = (string)this[0].Evaluate(context);
          int j = 0;
          for (int length = context.StackCount; j < length; j++) {
            value = context.ValueAt(j);
            if (HasProperty(value, name)) {
              break;
            }
          }
          if (j == context.StackCount) {
            value = new EcmaValue(context.Globals);
            if (!HasProperty(value, name)) {
              return false;
            }
          }
          value = value[name];
          break;
      }
      for (int i = 1, len = this.Count; i < len && !value.IsNullOrUndefined; i++) {
        string name = (string)this[i].Evaluate(context);
        value = value[name];
      }
      return valid;
    }

    private static bool HasProperty(EcmaValue value, string name) {
      if (value.IsNullOrUndefined) {
        return false;
      }
      int dummy;
      if (value.IsArrayLike && Int32.TryParse(name, out dummy)) {
        return true;
      }
      return value.HasProperty(name);
    }
  }

  [DebuggerDisplay("{Value,nq}")]
  internal class ConstantObjectPath : IObjectPath {
    public ConstantObjectPath(string str) {
      this.Value = str;
    }

    public string Value { get; private set; }

    public EcmaValue Evaluate(EvaluationContext context) {
      return this.Value;
    }
  }
}
