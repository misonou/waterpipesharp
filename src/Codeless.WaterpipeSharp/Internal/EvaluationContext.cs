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
    private readonly Stack<EvaluationStack> objStack = new Stack<EvaluationStack>();
    private readonly TokenList tokens;
    [ThreadStatic]
    private static int evalCount;
    [ThreadStatic]
    private static Stack<EvaluateOptions> execStack;

    private enum OutputMode { Undecided, String, RawValue }

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
      options = options.Clone();
      this.Globals = new PipeGlobal(options.Globals);
      this.Options = options;
      this.tokens = tokens;
      objStack.Push(new EvaluationStack(data));
      if (execStack == null) {
        execStack = new Stack<EvaluateOptions>();
      }
      if (options.IndentString == null && options.Indent > 0) {
        options.IndentString = Helper.Repeat(" ", options.Indent);
      }
      if (execStack.Count > 0) {
        var prevOptions = execStack.Peek();
        if (options.IndentPaddingString == null && (prevOptions.IndentLevel > 0 || prevOptions.IndentPaddingString != null)) {
          options.IndentPaddingString = Helper.Indent(prevOptions.IndentString, prevOptions.IndentLevel, prevOptions.IndentPaddingString);
        }
      }
      if (options.IndentPaddingString == null && options.IndentPadding > 0) {
        options.IndentPaddingString = Helper.Repeat(" ", options.IndentPadding);
      }
      execStack.Push(options);
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

    private object Evaluate() {
      EcmaValue result = default;
      OutputMode outputMode = this.Options.OutputRawValue ? OutputMode.Undecided : OutputMode.String;
      StringBuilder sb = new StringBuilder();
      string lastAppendStr = null;
      Action<string> append = s => {
        sb.Append(s);
        lastAppendStr = s;
      };
      try {
        int i = 0;
        int e = tokens.Count;
        string ws = "";
        while (i < e) {
          Token t = tokens[i++];
          Options.IndentLevel = t.Indent.Value;
          switch (t.Type) {
            case TokenType.OP_EVAL:
              EvaluateToken et = (EvaluateToken)t;
              int prevCount = evalCount;
              result = et.Expression.Evaluate(this);
              if (!result.IsNullOrUndefined) {
                outputMode = outputMode != OutputMode.Undecided ? OutputMode.String : OutputMode.RawValue;
                if (ws != null) {
                  append(ws);
                }
                ws = null;
                string str = Helper.String(result, Helper.Json);
                if (evalCount != prevCount || et.SuppressHtmlEncode) {
                  append(str);
                } else {
                  append(Helper.Escape(str, false));
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
            case TokenType.OP_SPACE:
              SpaceToken st = (SpaceToken)t;
              if (ws == "") {
                ws = null;
              } else if (st.Value == "\r\n" && Options.IndentString != null) {
                string k = "\r\n" + Helper.Indent(Options.IndentString, t.Indent.Value, Options.IndentPaddingString);
                if (k.IndexOf(lastAppendStr) == 0) {
                  sb.Remove(sb.Length - lastAppendStr.Length, lastAppendStr.Length);
                }
                append(k);
                ws = null;
              } else {
                ws = st.Value;
              }
              break;
            default:
              OutputToken ot = (OutputToken)t;
              if (ws != null && !ot.TrimStart) {
                append(ws);
              }
              append(ot.Value);
              outputMode = OutputMode.String;
              ws = ot.TrimEnd ? "" : null;
              break;
          }
        }
      } finally {
        evalCount = (evalCount + (outputMode == OutputMode.String ? 1 : 0)) & 0xFFFF;
        execStack.Pop();
      }
      if (outputMode == OutputMode.String) {
        if ((Options.Indent > 0 || Options.IndentString != null) && Options.IndentPaddingString != null && !Options.TrimStart) {
          sb.Insert(0, Options.IndentPaddingString);
        }
        return sb.ToString();
      }
      return result;
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
