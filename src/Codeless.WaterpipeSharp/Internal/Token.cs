using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Codeless.WaterpipeSharp.Internal {
  internal enum TokenType {
    OP_EVAL = 1,
    OP_TEST = 2,
    OP_ITER_END = 3,
    OP_ITER = 4,
    OP_JUMP = 5,
    OP_TEXT = 6,
    OP_SPACE = 7,
  }

  internal abstract class Token {
    public abstract TokenType Type { get; }
  }

  internal abstract class ControlToken : Token {
    public int Index { get; set; }
  }

  [DebuggerDisplay("@jump {Index}")]
  internal class BranchToken : ControlToken {
    public override TokenType Type {
      get { return TokenType.OP_JUMP; }
    }
  }

  [DebuggerDisplay("@if {Expression.TextValue} [@jump {Index}]")]
  internal class ConditionToken : ControlToken {
    public ConditionToken(Pipe pipe, bool negate) {
      Guard.ArgumentNotNull(pipe, "pipe");
      this.Expression = pipe;
      this.Negate = negate;
    }

    public bool Negate { get; private set; }
    public Pipe Expression { get; private set; }

    public override TokenType Type {
      get { return TokenType.OP_TEST; }
    }
  }

  [DebuggerDisplay("@foreach {Expression.TextValue}")]
  internal class IterationToken : ControlToken {
    public IterationToken(Pipe pipe) {
      Guard.ArgumentNotNull(pipe, "pipe");
      this.Expression = pipe;
    }

    public Pipe Expression { get; }

    public override TokenType Type {
      get { return TokenType.OP_ITER; }
    }
  }

  [DebuggerDisplay("@endforeach")]
  internal class IterationEndToken : ControlToken {
    public IterationEndToken(int index) {
      this.Index = index;
    }

    public override TokenType Type {
      get { return TokenType.OP_ITER_END; }
    }
  }

  [DebuggerDisplay("@eval {Expression.TextValue}")]
  internal class EvaluateToken : Token {
    public EvaluateToken(Pipe pipe, bool suppressEncode) {
      Guard.ArgumentNotNull(pipe, "pipe");
      this.Expression = pipe;
      this.SuppressHtmlEncode = suppressEncode;
    }

    public Pipe Expression { get; }
    public bool SuppressHtmlEncode { get; }

    public override TokenType Type {
      get { return TokenType.OP_EVAL; }
    }
  }

  internal abstract class OutputTokenBase : Token {
    public abstract string Value { get; set; }
  }

  [DebuggerDisplay("@space")]
  internal class SpaceToken : OutputTokenBase {
    public override TokenType Type => TokenType.OP_SPACE;
    public override string Value {
      get { return " "; }
      set { }
    }
  }

  [DebuggerDisplay("@out '{Value}'")]
  internal class OutputToken : OutputTokenBase {
    public override TokenType Type => TokenType.OP_TEXT;
    public int Index { get; set; }
    public override string Value { get; set; }
    public bool TrimStart { get; set; }
    public bool TrimEnd { get; set; }
    public string TagName { get; set; }
    public bool? TagOpened { get; set; }
    public string AttributeName { get; set; }
    public bool MuteTagEnd { get; set; }
  }
}
