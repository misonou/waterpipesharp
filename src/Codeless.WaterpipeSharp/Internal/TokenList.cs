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
    private static readonly Regex reConstruct = new Regex(@"\{\{([\/!]|foreach(?=\s|\})|if(?:\snot)?(?=\s)|else(?:if(?:\snot)?(?=\s))?|&?(?!\}))\s*((?:\}(?!\})|[^}])*)\}\}");
    private static readonly Regex reHtml = new Regex(@"<(\/?)([0-9a-z]+|!doctype|!--)|\/?>|-->|([^\s=\/<>""0-9.-][^\s=\/<>""]*)(?:=""|$|(?=[\s=\/<>""]))|""|\r?\n\s*", RegexOptions.IgnoreCase);
    private static readonly string[] VoidTags = new[] { "area", "base", "br", "col", "command", "embed", "hr", "img", "input", "keygen", "link", "meta", "param", "source", "track", "wbr", "!doctype", "!--" };

    private const string TOKEN_IF = "if";
    private const string TOKEN_IFNOT = "if not";
    private const string TOKEN_ELSE = "else";
    private const string TOKEN_ELSEIF = "elseif";
    private const string TOKEN_ELSEIFNOT = "elseif not";
    private const string TOKEN_FOREACH = "foreach";

    private static readonly ConcurrentDictionary<string, TokenList> cache = new ConcurrentDictionary<string, TokenList>();
    private readonly Stack<ControlTokenInfo> controlStack = new Stack<ControlTokenInfo>(new[] { new ControlTokenInfo("", 0, null, 0) });
    private readonly Stack<OutputToken> htmlStack = new Stack<OutputToken>(new[] { new OutputToken { TagOpened = true } });
    private readonly string inputString;

    private Match m;
    private int lastIndex = 0;
    private int htmlStartIndex;

    private TokenList(string inputString) {
      Guard.ArgumentNotNull(inputString, "inputString");
      this.inputString = inputString.Trim();
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
          ParseHtmlContent(inputString.Substring(lastIndex, m.Index - lastIndex), 0);
        }
        lastIndex = m.Index + m.Length;

        string m1 = m.Groups[1].Value;
        string m2 = m.Groups[2].Value;
        switch (m1) {
          case "!":
            break;
          case "/":
            Assert(controlStack.Count > 0 && m2 == controlStack.Peek().TokenName);
            ParseHtmlContent("");
            controlStack.Peek().Token.Index = this.Count;
            if (controlStack.Peek().TokenIf != null && controlStack.Peek().TokenIf.Index == 0) {
              controlStack.Peek().TokenIf.Index = this.Count;
            }
            if (m2 == TOKEN_FOREACH) {
              Add(new IterationEndToken(controlStack.Peek().TokenIndex + 1));
            }
            controlStack.Pop();
            break;
          case TOKEN_IF:
          case TOKEN_IFNOT: {
              ConditionToken t = new ConditionToken(CreatePipe(), m1 == TOKEN_IFNOT);
              controlStack.Push(new ControlTokenInfo(TOKEN_IF, this.Count, t, htmlStack.Count));
              Add(t);
              break;
            }
          case TOKEN_ELSE:
          case TOKEN_ELSEIF:
          case TOKEN_ELSEIFNOT: {
              Assert(controlStack.Count > 0 && controlStack.Peek().TokenName == TOKEN_IF);
              ParseHtmlContent("");
              ControlTokenInfo previousControl = controlStack.Pop();
              controlStack.Push(new ControlTokenInfo(TOKEN_IF, this.Count, new BranchToken(), htmlStack.Count));

              (previousControl.TokenIf ?? previousControl.Token).Index = this.Count + 1;
              if (previousControl.Token.Type == TokenType.OP_JUMP) {
                controlStack.Peek().Token = previousControl.Token;
              }
              Add(controlStack.Peek().Token);
              if (m1 == TOKEN_ELSEIF || m1 == TOKEN_ELSEIFNOT) {
                controlStack.Peek().TokenIf = new ConditionToken(CreatePipe(), m1 == TOKEN_ELSEIFNOT);
                Add(controlStack.Peek().TokenIf);
              }
              break;
            }
          case TOKEN_FOREACH: {
              IterationToken t = new IterationToken(CreatePipe());
              controlStack.Push(new ControlTokenInfo(TOKEN_FOREACH, this.Count, t, htmlStack.Count));
              Add(t);
              break;
            }
          default:
            Add(new EvaluateToken(CreatePipe(), m1 == "&") { Indent = htmlStack.Count - 1 });
            break;
        }
      }
      ParseHtmlContent(inputString.Substring(lastIndex), 1);
      for (int i = this.Count - 1, j = 0; i >= 0; i--) {
        if (this[i].Indent != null) {
          j = this[i].Indent.Value;
        } else {
          this[i].Indent = j;
        }
      }
    }

    private bool IsScriptOrStyle {
      get { return htmlStack.Peek().TagName == "script" || htmlStack.Peek().TagName == "style"; }
    }

    private void ParseHtmlContent(string str) {
      ParseHtmlContent(str, controlStack.Peek().HtmlStackCount);
    }

    private void ParseHtmlContent(string str, int htmlStackCount) {
      int start = this.Count;
      int lastIndex = 0;
      htmlStartIndex = start;

      for (Match m = reHtml.Match(str); m.Success; m = m.NextMatch()) {
        if (lastIndex != m.Index) {
          AppendTextContent(str.Substring(lastIndex, m.Index - lastIndex), false);
        }
        lastIndex = m.Index + m.Length;

        OutputToken current;
        switch (m.Value[0]) {
          case '<':
            if (this.IsScriptOrStyle && (!m.Groups[1].Success || m.Groups[2].Value.ToLower() != htmlStack.Peek().TagName)) {
              AppendTextContent(m.Value, true);
            } else {
              while (htmlStack.Peek().TagName != null && VoidTags.Contains(htmlStack.Peek().TagName) && TryPopHtmlStack()) ;
              current = htmlStack.Peek();
              if (m.Groups[1].Success && m.Groups[1].Value.Length > 0) {
                if (current.TagName != m.Groups[2].Value || htmlStack.Count <= Math.Max(controlStack.Peek().HtmlStackCount, 1)) {
                  current.MuteTagEnd = true;
                  current.TagOpened = null;
                } else {
                  current.TagOpened = false;
                  AppendTextContent(m.Value, true);
                }
              } else {
                htmlStack.Push(new OutputToken { TagName = m.Groups[2].Value.ToLower() });
                AppendTextContent(m.Value.ToLower(), true);
                htmlStack.Peek().Index = this.Count - 1;
              }
            }
            continue;
          case '>':
          case '/':
          case '-':
            current = htmlStack.Peek();
            if (current.TagName != null && (current.TagOpened != true || m.Value == "/>" || m.Value == "-->")) {
              if (current.MuteTagEnd) {
                current.MuteTagEnd = false;
              } else {
                AppendTextContent(m.Value, true);
              }
              if ((current.TagOpened != false && m.Value != "/>" && m.Value != "-->") || !TryPopHtmlStack()) {
                htmlStack.Peek().TagOpened = true;
              }
              continue;
            }
            break;
          case '"':
            current = htmlStack.Peek();
            if (current.AttributeName != null && TryPopHtmlStack()) {
              AppendTextContent(m.Value, true);
              continue;
            }
            break;
          default:
            current = htmlStack.Peek();
            if (current.TagName != null && current.TagOpened != true) {
              AppendTextContent(" " + m.Value, true);
              if (m.Value.IndexOf('=') >= 0) {
                htmlStack.Push(new OutputToken { AttributeName = m.Groups[3].Value });
              }
              continue;
            }
            break;
        }
        AppendTextContent(m.Value, false);
      }
      if (lastIndex != str.Length) {
        AppendTextContent(str.Substring(lastIndex), false);
      }
      if (htmlStackCount > 0) {
        while (htmlStack.Count > htmlStackCount) {
          string tagName = htmlStack.Pop().TagName;
          if (!VoidTags.Contains(tagName)) {
            AppendTextContent("</" + tagName + ">", true);
          }
        }
      }
    }

    private void AppendTextContent(string str, bool stripWS) {
      OutputToken current = htmlStack.Peek();
      if (this.IsScriptOrStyle) {
        Add(new OutputToken { Value = str });
        return;
      }
      if (stripWS || current.AttributeName != null || current.TagOpened == true) {
        Token last1 = this.Count >= 1 ? this[this.Count - 1] : null;
        Token last2 = this.Count >= 2 ? this[this.Count - 2] : null;
        bool newline = false;
        if (!stripWS) {
          newline = str.IndexOf('\n') >= 0;
          str = Helper.Escape(Regex.Replace(str, "\\s+", current.TagOpened == true || (current.AttributeName != null && current.Text != null) ? " " : ""), true);
          newline = newline && (str != "" || str == " ");
        }
        current.Text += str;
        if (newline) {
          last1.TrimEnd = false;
          Add(new SpaceToken { Value = "\r\n" });
        } else if (str != "" && ((htmlStack.Count > 1 && current.TagOpened == true) || str != " ")) {
          OutputToken last1t = last1 as OutputToken;
          if (this.Count > htmlStartIndex && last1t != null) {
            last1t.Value += str;
            last1.TrimEnd = stripWS;
          } else if (this.Count > htmlStartIndex + 1 && last2 is OutputToken last2t && last1t != null && last1t.Value != "\r\n") {
            last2t.Value += (stripWS || last2t.TrimEnd || last1 == null ? "" : last1t.Value) + str;
            last2.TrimEnd = stripWS;
            Remove(last1);
          } else {
            Add(new OutputToken {
              Value = str,
              TrimStart = stripWS,
              TrimEnd = stripWS,
              Indent = htmlStack.Count - 2 + (current.TagOpened == true ? 1 : 0)
            });
          }
        } else {
          Add(new SpaceToken { Value = " " });
        }
      }
    }

    private bool TryPopHtmlStack() {
      if (htmlStack.Count > controlStack.Peek().HtmlStackCount) {
        htmlStack.Pop();
        return true;
      }
      return false;
    }

    private class ControlTokenInfo {
      public ControlTokenInfo(string tokenType, int index, ControlToken token, int htmlStackCount) {
        this.TokenName = tokenType;
        this.TokenIndex = index;
        this.Token = token;
        this.HtmlStackCount = htmlStackCount;
      }

      public int TokenIndex { get; }
      public string TokenName { get; }
      public int HtmlStackCount { get; }
      public ControlToken Token { get; set; }
      public ControlToken TokenIf { get; set; }
    }
  }
}
