using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Codeless.WaterpipeSharp.Internal {
  internal enum TokenType {
    OP_EVAL = 1,
    OP_TEST = 2,
    OP_ITER_END = 3,
    OP_ITER = 4,
    OP_JUMP = 5,
    HTMLOP_ELEMENT_START = 6,
    HTMLOP_ELEMENT_END = 7,
    HTMLOP_ATTR_END = 8,
    HTMLOP_ATTR_START = 9,
    HTMLOP_TEXT = 10,
  }

  internal abstract class Token {
    public abstract TokenType Type { get; }
  }

  internal abstract class ControlToken : Token {
    public int Index { get; set; }
  }

  internal class BranchToken : ControlToken {
    public override TokenType Type {
      get { return TokenType.OP_JUMP; }
    }
  }

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

  internal class IterationEndToken : ControlToken {
    public IterationEndToken(int index) {
      this.Index = index;
    }

    public override TokenType Type {
      get { return TokenType.OP_ITER_END; }
    }
  }

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

  internal class OutputToken : Token {
    public OutputToken(TokenType type) {
      this.Type = type;
    }

    public override TokenType Type { get; }
    public int Index { get; set; }
    public string Value { get; internal set; }
  }

  internal class HtmlOutputToken : OutputToken {
    public HtmlOutputToken(TokenType type)
      : base(type) { }

    public string TagName { get; set; }
    public bool TagOpened { get; set; }
    public Dictionary<string, HtmlOutputToken> Attributes { get; } = new Dictionary<string, HtmlOutputToken>();

    public string AttributeName { get; set; }

    public string Text { get; set; }
    public List<int> Offsets { get; set; }
  }
}
