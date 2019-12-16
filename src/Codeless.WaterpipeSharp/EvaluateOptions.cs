using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Codeless.WaterpipeSharp {
  /// <summary>
  /// Specifies the behavior of template evaluation.
  /// </summary>
  public class EvaluateOptions {
    /// <summary>
    /// Instantiates an instance of the <see cref="EvaluateOptions"/> with default options.
    /// </summary>
    public EvaluateOptions() {
      this.Globals = new PipeGlobal();
    }

    public EvaluateOptions(EvaluateOptions options, PipeGlobal parent) {
      this.Indent = options.Indent;
      this.IndentString = options.IndentString;
      this.IndentPadding = options.IndentPadding;
      this.IndentPaddingString = options.IndentPaddingString;
      this.Globals = new PipeGlobal(parent);
    }

    /// <summary>
    /// Gets the collection of global values that is passed to the template evaluation.
    /// </summary>
    public PipeGlobal Globals { get; set; }
    public int Indent { get; set; }
    public string IndentString { get; set; }
    public int IndentPadding { get; set; }
    public string IndentPaddingString { get; set; }

    internal bool OutputRawValue { get; set; }
    internal bool TrimStart { get; set; }
    internal int IndentLevel { get; set; }

    internal EvaluateOptions Clone() {
      return (EvaluateOptions)MemberwiseClone();
    }
  }
}
