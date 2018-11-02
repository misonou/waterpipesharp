using Codeless.Ecma;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Codeless.WaterpipeSharp.Internal {
  internal enum PipeArgumentEvaluationMode {
    Auto,
    Constant,
    Evaluated
  }

  [DebuggerDisplay("{TextValue,nq}")]
  internal class PipeArgument {
    private ObjectPath objectPath;
    private int? length;

    public PipeArgument(string str, int startIndex, int endIndex, PipeArgumentEvaluationMode canEvaluate) {
      this.TextValue = str;
      this.StartIndex = startIndex;
      this.EndIndex = endIndex;
      this.Value = ParseValue(str);
      this.EvaluationMode = this.Value.Type != EcmaValueType.String ? PipeArgumentEvaluationMode.Constant : canEvaluate;
    }

    public int StartIndex { get; }
    public int EndIndex { get; }
    public string TextValue { get; }
    public EcmaValue Value { get; }
    public PipeArgument Next { get; set; }
    public PipeArgumentEvaluationMode EvaluationMode { get; }

    public int Length {
      get {
        if (length == null) {
          length = 0;
          if (this.TextValue == "[" && this.EvaluationMode == PipeArgumentEvaluationMode.Auto) {
            PipeArgument t = this.Next;
            int i = 1;
            int count = 1;
            for (; t != null; t = t.Next, i++) {
              if (t.TextValue == "[") {
                count++;
              } else if (t.TextValue == "]" && --count == 0) {
                length = i;
                break;
              }
            }
          }
        }
        return length.Value;
      }
    }

    public ObjectPath ObjectPath {
      get {
        if (objectPath == null) {
          objectPath = ObjectPath.FromString(this.TextValue);
        }
        return objectPath;
      }
    }

    private static EcmaValue ParseValue(string str) {
      switch (str) {
        case "true":
          return true;
        case "false":
          return false;
        case "undefined":
          return EcmaValue.Undefined;
        case "null":
          return EcmaValue.Null;
        case "0":
          return 0;
      }
      EcmaValue number = +new EcmaValue(str);
      if (!Double.NaN.Equals(number.GetUnderlyingObject())) {
        return number;
      }
      return str;
    }
  }
}
