using Codeless.Ecma;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace Codeless.WaterpipeSharp.Internal {
  internal class EvaluationContext {
    private static readonly ConcurrentDictionary<string, PipeFunction> functions = new ConcurrentDictionary<string, PipeFunction>();
    private static readonly List<PipeFunctionResolver> resolvers = new List<PipeFunctionResolver>() { DefaultNativeFunctionResolver };

    private readonly List<PipeExecutionException> exceptions = new List<PipeExecutionException>();
    private readonly Stack<XmlElement> xmlStack = new Stack<XmlElement>();
    private readonly Stack<EvaluationStack> objStack = new Stack<EvaluationStack>();
    private readonly EcmaValue data;
    private readonly TokenList tokens;
    private int evalCount;

    static EvaluationContext() {
      foreach (MethodInfo method in typeof(BuiltInPipeFunction).GetMethods(BindingFlags.Static | BindingFlags.Public)) {
        BuiltInPipeFunctionAliasAttribute attribute = method.GetCustomAttributes(false).OfType<BuiltInPipeFunctionAliasAttribute>().FirstOrDefault();
        if (attribute == null || !attribute.UseAliasOnly) {
          RegisterFunction(method.Name.ToLowerInvariant(), PipeFunction.Create(method));
        }
        if (attribute != null) {
          RegisterFunction(attribute.Alias, PipeFunction.Create(method));
        }
      }
      RegisterFunctionResolver(BuiltInPipeFunction.ResolveFunction);
    }

    private EvaluationContext(TokenList tokens, EcmaValue data, EvaluateOptions options) {
      Guard.ArgumentNotNull(tokens, "tokens");
      Guard.ArgumentNotNull(options, "options");
      this.Options = options;
      this.tokens = tokens;
      this.data = data;
      objStack.Push(new EvaluationStack(data));

      if (options.OutputXml) {
        XmlDocument doc = new XmlDocument();
        xmlStack.Push(doc.DocumentElement);
      }
      this.Globals = new PipeGlobal(options.Globals);
    }

    public EvaluateOptions Options { get; }

    public PipeGlobal Globals { get; }

    public int StackCount {
      get { return objStack.Count; }
    }

    public string InputString {
      get { return tokens.Value; }
    }

    public EvaluationStack Current {
      get { return objStack.Peek(); }
    }

    public XmlElement CurrentXmlElement {
      get { return xmlStack.Peek(); }
    }

    public PipeExecutionException[] Exceptions {
      get { return exceptions.ToArray(); }
    }

    public EcmaValue ValueAt(int index) {
      if (index >= objStack.Count) {
        return EcmaValue.Undefined;
      }
      return ((IEnumerable<EvaluationStack>)objStack).ElementAt(index).Value;
    }

    public void Push(EcmaValue obj) {
      objStack.Push(new EvaluationStack(obj));
    }

    public void Push(EcmaValue obj, EcmaValue index) {
      objStack.Push(new EvaluationStack(new EcmaValue(new Hashtable { { index.ToString(), obj } })));
      objStack.Peek().MoveNext();
    }

    public void Pop() {
      objStack.Pop();
    }

    public void AddException(PipeExecutionException ex) {
      exceptions.Add(ex);
    }

    private object Evaluate() {
      StringBuilder sb = new StringBuilder();
      try {
        int i = 0;
        int e = tokens.Count;
        while (i < e) {
          Token t = tokens[i++];
          switch (t.Type) {
            case TokenType.OP_EVAL:
              EvaluateToken et = (EvaluateToken)t;
              int prevCount = evalCount;
              EcmaValue result = et.Expression.Evaluate(this);
              if (!result.IsNullOrUndefined && !result.IsNaN) {
                string str = result.Type == EcmaValueType.Object ? Json.Stringify(result, EcmaValue.Undefined, EcmaValue.Undefined) : result.ToString();
                if (evalCount != prevCount || et.SuppressHtmlEncode) {
                  sb.Append(str);
                } else {
                  sb.Append(HttpUtility.HtmlEncode(str));
                }
              }
              break;
            case TokenType.OP_ITER:
              IterationToken it = (IterationToken)t;
              objStack.Push(new EvaluationStack(it.Expression.Evaluate(this)));
              if (!objStack.Peek().MoveNext()) {
                i = it.Index;
              }
              break;
            case TokenType.OP_ITER_END:
              IterationEndToken iet = (IterationEndToken)t;
              if (!objStack.Peek().MoveNext()) {
                Pop();
              } else {
                i = iet.Index;
              }
              break;
            case TokenType.OP_TEST:
              ConditionToken ct = (ConditionToken)t;
              if (!(bool)ct.Expression.Evaluate(this) ^ ct.Negate) {
                i = ct.Index;
              }
              break;
            case TokenType.OP_JUMP:
              i = ((BranchToken)t).Index;
              break;
            default:
              OutputToken ot = (OutputToken)t;
              if (this.Options.OutputXml) {
                XmlElement currentElm = xmlStack.Peek();
                HtmlOutputToken ht = (HtmlOutputToken)ot;
                switch (t.Type) {
                  case TokenType.HTMLOP_ELEMENT_START:
                    FlushOutput(sb, currentElm);
                    xmlStack.Push(currentElm.OwnerDocument.CreateElement(ht.TagName));
                    currentElm.AppendChild(xmlStack.Peek());
                    break;
                  case TokenType.HTMLOP_ELEMENT_END:
                    FlushOutput(sb, currentElm);
                    xmlStack.Pop();
                    break;
                  case TokenType.HTMLOP_ATTR_END:
                    XmlAttribute attr = currentElm.OwnerDocument.CreateAttribute(ht.AttributeName);
                    attr.Value = sb.ToString();
                    currentElm.Attributes.Append(attr);
                    sb.Remove(0, sb.Length);
                    break;
                  case TokenType.HTMLOP_TEXT:
                    sb.Append(ht.Text);
                    break;
                }
              } else {
                sb.Append(ot.Value);
                i = ot.Index;
              }
              break;
          }
        }
      } finally {
        evalCount = (evalCount + 1) & 0xFFFF;
      }
      if (this.Options.OutputXml) {
        FlushOutput(sb, xmlStack.Peek());
        return xmlStack.Peek();
      }
      return Regex.Replace(sb.ToString(), @"^\s+(?=<)|(>)\s+(<|$)", m => m.Groups[1].Value + m.Groups[2].Value);
    }

    public PipeFunction ResolveFunction(string name) {
      GetNextPipeFunctionResolverDelegate getNext = new PipeFunctionResolverEnumerator().GetNext;
      return getNext()(name, getNext);
    }

    public static void RegisterFunction(string name, PipeFunction fn) {
      Guard.ArgumentNotNull(name, "name");
      Guard.ArgumentNotNull(fn, "fn");
      functions.TryAdd(name, fn);
    }

    public static void RegisterFunctionResolver(PipeFunctionResolver resolver) {
      Guard.ArgumentNotNull(resolver, "resolver");
      resolvers.Insert(1, resolver);
    }

    public static object Evaluate(string template, EcmaValue value, EvaluateOptions options, out PipeExecutionException[] exceptions) {
      TokenList tokens = TokenList.FromString(template);
      return Evaluate(tokens, value, options, out exceptions);
    }

    public static object Evaluate(TokenList tokens, EcmaValue value, EvaluateOptions options, out PipeExecutionException[] exceptions) {
      Guard.ArgumentNotNull(tokens, "tokens");
      Guard.ArgumentNotNull(options, "options");
      EvaluationContext context = new EvaluationContext(tokens, value, options);
      object returnValue = context.Evaluate();
      exceptions = context.Exceptions;
      return returnValue;
    }

    private static void FlushOutput(StringBuilder sb, XmlElement currentElm) {
      if (sb.Length > 0) {
        currentElm.InnerText = sb.ToString();
        sb.Remove(0, sb.Length);
      }
    }

    private static PipeFunction DefaultNativeFunctionResolver(string name, GetNextPipeFunctionResolverDelegate getNext) {
      PipeFunction fn;
      if (functions.TryGetValue(name, out fn)) {
        return fn;
      }
      return getNext()(name, getNext);
    }

    private static PipeFunction ThrowInvalidPipeFunctionException(string name, GetNextPipeFunctionResolverDelegate getNext) {
      return null;
    }

    private class PipeFunctionResolverEnumerator {
      private readonly IEnumerator<PipeFunctionResolver> enumerator;

      public PipeFunctionResolverEnumerator() {
        this.enumerator = ((IEnumerable<PipeFunctionResolver>)new List<PipeFunctionResolver>(resolvers)).GetEnumerator();
      }

      public PipeFunctionResolver GetNext() {
        if (enumerator.MoveNext()) {
          return enumerator.Current;
        }
        return ThrowInvalidPipeFunctionException;
      }
    }
  }
}
