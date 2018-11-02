using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace Codeless.WaterpipeSharp.Internal {
  [Serializable]
  internal class ParseException : WaterpipeException {
    public ParseException(string message, CallSite callsite)
      : base(message, callsite) { }
  }

  [DebuggerDisplay("{Value,nq}")]
  internal class TokenList : Collection<Token> {
    private static readonly Regex reConstruct = new Regex(
      @"\{\{([\/!]|foreach(?=\s|\})|if(?:\snot)?(?=\s)|else(?:if(?:\snot)?(?=\s))?|&?(?!\}))\s*((?:\}(?!\})|[^}])*)\}\}");
    private static readonly Regex reHtml = new Regex(
      @"<(\/?)([^\s>\/]+)|\/?>|(\w+)(?:(?=[\s>\/])|="")|""");
    private static readonly string[] VoidTags = new[] { "area", "base", "br", "col", "command", "embed", "hr", "img", "input", "keygen", "link", "meta", "param", "source", "track", "wbr" };

    private const string TOKEN_IF = "if";
    private const string TOKEN_IFNOT = "if not";
    private const string TOKEN_ELSE = "else";
    private const string TOKEN_ELSEIF = "elseif";
    private const string TOKEN_ELSEIFNOT = "elseif not";
    private const string TOKEN_FOREACH = "foreach";

    private static readonly ConcurrentDictionary<string, TokenList> cache = new ConcurrentDictionary<string, TokenList>();
    private readonly Stack<ControlTokenInfo> controlStack = new Stack<ControlTokenInfo>();
    private readonly Stack<HtmlOutputToken> htmlStack = new Stack<HtmlOutputToken>();
    private readonly string inputString;

    private Match m;
    private int lastIndex = 0;

    private TokenList(string inputString) {
      Guard.ArgumentNotNull(inputString, "inputString");
      this.inputString = inputString;
      Parse();
    }

    public string Value {
      get { return this.inputString; }
    }

    public static TokenList FromString(string str) {
      TokenList value;
      if (!cache.TryGetValue(str, out value)) {
        value = new TokenList(str);
        cache.TryAdd(str, value);
      }
      return value;
    }

    private void Assert(bool result) {
      if (!result) {
        throw new ParseException("Unexpected " + m.Value, new WaterpipeException.CallSite {
          InputString = inputString,
          ConstructStart = m.Index,
          ConstructEnd = m.Index + m.Length
        });
      }
    }

    private Pipe CreatePipe() {
      string str = m.Groups[2].Value;
      return new Pipe(str, m.Index + m.Value.IndexOf(str));
    }

    private void Parse() {
      for (m = reConstruct.Match(inputString); m.Success; m = m.NextMatch()) {
        if (lastIndex != m.Index) {
          ParseTextContent(inputString.Substring(lastIndex, m.Index - lastIndex));
        }
        lastIndex = m.Index + m.Length;

        string m1 = m.Groups[1].Value;
        string m2 = m.Groups[2].Value;
        switch (m1) {
          case "!":
            break;
          case "/":
            Assert(controlStack.Count > 0 && m2 == controlStack.Peek().TokenName);
            controlStack.Peek().Token.Index = this.Count;
            if (controlStack.Peek().TokenIf != null && controlStack.Peek().TokenIf.Index == 0) {
              controlStack.Peek().TokenIf.Index = this.Count;
            }
            if (m2 == TOKEN_FOREACH) {
              this.Add(new IterationEndToken(controlStack.Peek().TokenIndex + 1));
            }
            controlStack.Pop();
            break;
          case TOKEN_IF:
          case TOKEN_IFNOT: {
              ConditionToken t = new ConditionToken(CreatePipe(), m1 == TOKEN_IFNOT);
              controlStack.Push(new ControlTokenInfo(TOKEN_IF, this.Count, t));
              this.Add(t);
              break;
            }
          case TOKEN_ELSE:
          case TOKEN_ELSEIF:
          case TOKEN_ELSEIFNOT: {
              Assert(controlStack.Count > 0 && controlStack.Peek().TokenName == TOKEN_IF);
              ControlTokenInfo previousControl = controlStack.Pop();
              controlStack.Push(new ControlTokenInfo(TOKEN_IF, this.Count, new BranchToken()));

              (previousControl.TokenIf ?? previousControl.Token).Index = this.Count + 1;
              if (previousControl.Token.Type == TokenType.OP_JUMP) {
                controlStack.Peek().Token = previousControl.Token;
              }
              this.Add(controlStack.Peek().Token);
              if (m1 == TOKEN_ELSEIF || m1 == TOKEN_ELSEIFNOT) {
                controlStack.Peek().TokenIf = new ConditionToken(CreatePipe(), m1 == TOKEN_ELSEIFNOT);
                this.Add(controlStack.Peek().TokenIf);
              }
              break;
            }
          case TOKEN_FOREACH: {
              IterationToken t = new IterationToken(CreatePipe());
              controlStack.Push(new ControlTokenInfo(TOKEN_FOREACH, this.Count, t));
              this.Add(t);
              break;
            }
          default:
            this.Add(new EvaluateToken(CreatePipe(), m1 == "&"));
            break;
        }
      }
      if (lastIndex != inputString.Length) {
        ParseTextContent(inputString.Substring(lastIndex));
      }
    }

    private void ParseTextContent(string str) {
      int start = this.Count;
      int lastIndex = 0;
      for (Match m = reHtml.Match(str); m.Success; m = m.NextMatch()) {
        HtmlOutputToken current = htmlStack.Count > 0 ? htmlStack.Peek() : null;
        if (lastIndex != m.Index) {
          AppendTextContent(str.Substring(lastIndex, m.Index - lastIndex));
        }
        lastIndex = m.Index + m.Length;

        switch (m.Value[0]) {
          case '<':
            if (m.Groups[1].Success) {
              if (current != null) {
                if (current.TagName == m.Groups[2].Value) {
                  current.TagOpened = false;
                } else if (VoidTags.Contains(current.TagName)) {
                  htmlStack.Pop();
                  if (htmlStack.Count > 0) {
                    current = htmlStack.Peek();
                  } else {
                    AppendTextContent(m.Value);
                  }
                }
              } else {
                AppendTextContent(m.Value);
              }
            } else {
              EndTextContent();
              htmlStack.Push(new HtmlOutputToken(TokenType.HTMLOP_ELEMENT_START) { TagName = m.Groups[2].Value });
              Add(htmlStack.Peek());
            }
            break;
          case '>':
          case '/':
            if (current != null && current.TagName != null) {
              if (!current.TagOpened || m.Value == "/>") {
                EndTextContent();
                Add(new HtmlOutputToken(TokenType.HTMLOP_ELEMENT_END));
                htmlStack.Pop();
                StartTextContent();
              } else if (m.Value == ">") {
                current.TagOpened = true;
                StartTextContent();
              }
            } else {
              AppendTextContent(m.Value);
            }
            break;
          case '"':
            if (current != null && current.AttributeName != null) {
              EndTextContent();
              Add(new HtmlOutputToken(TokenType.HTMLOP_ATTR_END) { AttributeName = current.AttributeName });
              htmlStack.Pop();
            } else {
              AppendTextContent(m.Value);
            }
            break;
          default:
            if (current != null) {
              if (m.Value.IndexOf('=') < 0) {
                Add(new HtmlOutputToken(TokenType.HTMLOP_ATTR_END) { AttributeName = m.Groups[3].Value });
              } else {
                htmlStack.Push(new HtmlOutputToken(TokenType.HTMLOP_ATTR_START) { AttributeName = m.Groups[3].Value });
                current.Attributes.Add(m.Groups[3].Value, htmlStack.Peek());
                Add(htmlStack.Peek());
                StartTextContent();
              }
            } else {
              AppendTextContent(m.Value);
            }
            break;
        }
      }
      if (lastIndex != str.Length) {
        AppendTextContent(str.Substring(lastIndex));
      }
      if (start < this.Count) {
        HtmlOutputToken p = this[start] as HtmlOutputToken;
        if (p != null) {
          p.Value = str;
          p.Index = this.Count;
        }
      }
    }

    private void StartTextContent() {
      if (htmlStack.Count > 0) {
        htmlStack.Peek().Offsets.Add(this.Count);
      }
    }

    private void EndTextContent() {
      if (htmlStack.Count > 0) {
        List<int> offsets = htmlStack.Peek().Offsets;
        if (offsets.Count > 0 && offsets.Last() == this.Count) {
          offsets.RemoveAt(offsets.Count - 1);
        } else {
          offsets.Add(this.Count);
        }
      }
    }

    private void AppendTextContent(string str) {
      if (htmlStack.Count > 0) {
        HtmlOutputToken current = htmlStack.Peek();
        if (current.AttributeName != null || current.TagOpened) {
          Add(new HtmlOutputToken(TokenType.HTMLOP_TEXT) { Text = str });
        }
      } else {
        Add(new HtmlOutputToken(TokenType.HTMLOP_TEXT) { Text = str });
      }
    }

    private class ControlTokenInfo {
      public ControlTokenInfo(string tokenType, int index, ControlToken token) {
        this.TokenName = tokenType;
        this.TokenIndex = index;
        this.Token = token;
      }

      public int TokenIndex { get; }
      public string TokenName { get; }
      public ControlToken Token { get; set; }
      public ControlToken TokenIf { get; set; }
    }
  }
}
