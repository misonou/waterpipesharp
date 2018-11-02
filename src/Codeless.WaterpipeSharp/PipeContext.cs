using Codeless.Ecma;
using Codeless.WaterpipeSharp.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.UI;

namespace Codeless.WaterpipeSharp {
  public class PipeExecutionException : WaterpipeException {
    internal PipeExecutionException(string message, Exception innerException, CallSite callsite)
      : base(message, innerException, callsite) { }
  }

  public class PipeContext {
    private readonly EvaluationContext context;
    private readonly List<EcmaValue> returnArray = new List<EcmaValue>();
    private readonly Pipe pipe;
    private readonly int start;
    private readonly int end;
    private EcmaValue input;
    private EcmaValue value;
    private int resetPos;
    private int i;

    internal PipeContext(EvaluationContext context, Pipe pipe) {
      Guard.ArgumentNotNull(context, "context");
      Guard.ArgumentNotNull(pipe, "pipe");
      this.context = context;
      this.pipe = pipe;
      this.end = pipe.Count - 1;
    }

    private PipeContext(PipeContext instance, int start, int end)
      : this(instance.context, instance.pipe) {
      this.start = start;
      this.end = end;
    }

    public EcmaValue Value { get { return value; } }

    internal EvaluationContext EvaluationContext {
      get { return context; }
    }

    public PipeGlobal Globals {
      get { return context.Globals; }
    }

    public bool HasArgument {
      get { return i <= end; }
    }

    public EcmaValue Evaluate(string objectPath) {
      return ObjectPath.FromString(objectPath).Evaluate(context);
    }

    public EcmaValue Stop() {
      i = end + 1;
      return this.Value;
    }

    public EcmaValue Push(EcmaValue value) {
      returnArray.Add(value);
      return value;
    }

    public EcmaValue Reset() {
      resetPos = i;
      return TakeArgument();
    }

    public EcmaValue TakeArgumentAsRaw() {
      if (i <= end) {
        PipeLambda fn = TakeLambda();
        if (fn != null) {
          return new EcmaValue(fn);
        }
        return pipe[i++].Value;
      }
      return EcmaValue.Undefined;
    }

    public EcmaValue TakeArgument() {
      return TakeArgument(true);
    }

    public EcmaValue TakeArgument(bool evaluateLambda) {
      bool reset = resetPos == i;
      if (i > end) {
        return reset ? input : EcmaValue.Undefined;
      }
      if (pipe[i].EvaluationMode == PipeArgumentEvaluationMode.Constant) {
        return pipe[i++].Value;
      }
      if (evaluateLambda) {
        PipeLambda fn = TakeLambda();
        if (fn != null) {
          return fn.Invoke(reset ? input : this.Value);
        }
      }
      EcmaValue value;
      bool valid = pipe[i].ObjectPath.TryEvaluate(context, reset, out value);
      if (pipe[i].EvaluationMode == PipeArgumentEvaluationMode.Evaluated || (valid && (reset || !this.Value.IsPrimitive || value.IsPrimitive))) {
        ++i;
        return value;
      }
      return reset ? input : pipe[i++].Value;
    }

    public PipeLambda TakeLambda() {
      return TakeLambda(null);
    }

    public PipeLambda TakeLambda(PipeLambdaFactory factory) {
      int start = i;
      if (start <= end && pipe[start].Length > 0) {
        int len = pipe[start].Length;
        i += len + 1;
        return new PipeLambdaHostClass(new PipeContext(this, start + 1, start + len - 1)).Invoke;
      }
      if (factory != null) {
        return factory.CreateLambda(TakeArgument());
      }
      return null;
    }

    internal EcmaValue Evaluate() {
      i = start;
      input = ObjectPath.Empty.Evaluate(context);
      value = Reset();
      while (i <= end) {
        int startpos = i;
        string name = pipe[i++].TextValue;
        try {
          PipeFunction fn = context.ResolveFunction(name);
          if (fn != null) {
            value = fn.Invoke(this);
          } else if (startpos == resetPos) {
            value = pipe[i - 1].EvaluationMode == PipeArgumentEvaluationMode.Constant ? pipe[i - 1].Value : EcmaValue.Undefined;
          } else {
            value = EcmaValue.Undefined;
            throw new Exception("Invalid pipe function");
          }
        } catch (Exception ex) {
          if (ex is TargetInvocationException) {
            ex = ex.InnerException;
          }
          PipeExecutionException wrappedException = new PipeExecutionException(ex.Message, ex, new WaterpipeException.CallSite {
            InputString = context.InputString,
            ConstructStart = pipe.StartIndex,
            ConstructEnd = pipe.EndIndex,
            HighlightStart = pipe[startpos].StartIndex,
            HighlightEnd = pipe[i - 1].EndIndex
          });
          context.AddException(wrappedException);
        }
      }
      if (returnArray.Count > 0) {
        returnArray.Add(value);
        return PipeValueExtension.Flatten(new EcmaValue(returnArray));
      }
      return value;
    }

    private class PipeLambdaHostClass {
      private readonly PipeContext pipe;

      public PipeLambdaHostClass(PipeContext pipe) {
        this.pipe = pipe;
      }

      public EcmaValue Invoke(EcmaValue obj, EcmaValue index) {
        try {
          pipe.EvaluationContext.Push(obj, index);
          return pipe.Evaluate();
        } finally {
          pipe.EvaluationContext.Pop();
        }
      }
    }
  }
}
