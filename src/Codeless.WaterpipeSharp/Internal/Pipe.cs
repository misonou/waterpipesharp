using Codeless.Ecma;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Codeless.WaterpipeSharp.Internal {
  [DebuggerDisplay("{TextValue,nq}")]
  internal class Pipe : Collection<PipeArgument> {
    private static readonly Regex reArgument = new Regex(@"([^\s\\""]|\\.)(?:[^\s\\]|\\.)*|""((?:[^""\\]|\\.)*)""");

    public Pipe(string str, int index) {
      Guard.ArgumentNotNull(str, "str");
      for (Match m = reArgument.Match(str); m.Success; m = m.NextMatch()) {
        string value = Regex.Replace((m.Groups[2].Success ? m.Groups[2] : m.Groups[0]).Value, "\\\\(.)", "$1");
        PipeArgumentEvaluationMode canEvaluate = m.Groups[2].Success ? PipeArgumentEvaluationMode.Constant : (m.Groups[1].Value[0] == '$' ? PipeArgumentEvaluationMode.Evaluated : PipeArgumentEvaluationMode.Auto);
        Add(new PipeArgument(value, index + m.Index, index + m.Index + m.Length, canEvaluate));
      }
      this.TextValue = str;
      this.StartIndex = index;
      this.EndIndex = index + str.Length;
    }

    public int StartIndex { get; }
    public int EndIndex { get; }
    public string TextValue { get; }

    public EcmaValue Evaluate(EvaluationContext context) {
      return new PipeContext(context, this).Evaluate();
    }

    protected override void InsertItem(int index, PipeArgument item) {
      if (index > 0) {
        this[index - 1].Next = item;
      }
      base.InsertItem(index, item);
    }
  }
}
