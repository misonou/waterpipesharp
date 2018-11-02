using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Codeless.WaterpipeSharp {
  /// <summary>
  /// Represents errors that occur during evaluation of the template.
  /// </summary>
  public abstract class WaterpipeException : Exception {
    private const string CallSiteTemplate = " at line {{$0}} column {{$1}}:\n\t{{&$2}}{{&$3}}\n\t{{$2.length + $4 repeat \\ }}{{$5 repeat ^}}";

    internal WaterpipeException(string message, CallSite callsite)
      : base(message + callsite.ToString()) { }

    internal WaterpipeException(string message, Exception innerException, CallSite callsite)
      : base(message + callsite.ToString(), innerException) { }

    internal class CallSite {
      public string InputString { get; set; }
      public int ConstructStart { get; set; }
      public int ConstructEnd { get; set; }
      public int? HighlightStart { get; set; }
      public int? HighlightEnd { get; set; }

      public override string ToString() {
        int caretStart = this.HighlightStart.GetValueOrDefault(this.ConstructStart);
        int newLinePos = this.InputString.LastIndexOf('\n', this.ConstructStart) + 1;
        string[] arr = this.InputString.Substring(0, caretStart).Split('\n');
        string lineStart = Regex.Replace(this.InputString.Substring(newLinePos, this.ConstructStart - newLinePos), @"^\s+", "");
        lineStart = lineStart.Substring(Math.Max(0, lineStart.Length - 20));
        int endPos = this.InputString.IndexOf('\n', this.ConstructEnd) + 1;
        if (endPos == 0) {
          endPos = this.InputString.Length;
        }
        string line = Regex.Replace(this.InputString.Substring(this.ConstructStart, endPos - this.ConstructStart), @"\r?\n|\s+$", "");
        line = line.Substring(0, Math.Min(line.Length, this.ConstructEnd - this.ConstructStart + 20));

        return Waterpipe.Evaluate(CallSiteTemplate,
          new object[] {
            arr.Length,
            arr.Last().Length + 1,
            lineStart,
            line,
            caretStart - this.ConstructStart,
            this.HighlightStart.HasValue ? this.HighlightEnd - this.HighlightStart : this.ConstructEnd - this.ConstructStart });
      }
    }
  }
}
