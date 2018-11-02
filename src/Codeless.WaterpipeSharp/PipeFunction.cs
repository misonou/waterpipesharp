using Codeless.Ecma;
using Codeless.WaterpipeSharp.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.UI;
using System.Xml;

namespace Codeless.WaterpipeSharp {
  /// <summary>
  /// An abstract class representing a pipe function that can be invoked with a given pipe context.
  /// </summary>
  public abstract class PipeFunction {
    /// <summary>
    /// Encapsulates a native method that takes arbitrary number of arguments or function arguments from pipe.
    /// </summary>
    /// <param name="context">Pipe context.</param>
    /// <returns>Result of the pipe function.</returns>
    public delegate EcmaValue Variadic(PipeContext context);

    /// <summary>
    /// Encapsulates a native method that takes no arguments from pipe.
    /// </summary>
    /// <param name="value">Piped value from object path or previous pipe function.</param>
    /// <returns>Result of the pipe function.</returns>
    public delegate EcmaValue Func0(EcmaValue value);

    /// <summary>
    /// Encapsulates a native method that takes one argument from pipe.
    /// </summary>
    /// <param name="value">Piped value from object path or previous pipe function.</param>
    /// <param name="arg1">First argument from pipe.</param>
    /// <returns>Result of the pipe function.</returns>
    public delegate EcmaValue Func1(EcmaValue value, EcmaValue arg1);

    /// <summary>
    /// Encapsulates a native method that takes two arguments from pipe.
    /// </summary>
    /// <param name="value">Piped value from object path or previous pipe function.</param>
    /// <param name="arg1">First argument from pipe.</param>
    /// <param name="arg2">Second argument from pipe.</param>
    /// <returns>Result of the pipe function.</returns>
    public delegate EcmaValue Func2(EcmaValue value, EcmaValue arg1, EcmaValue arg2);

    /// <summary>
    /// Encapsulates a native method that takes three arguments from pipe.
    /// </summary>
    /// <param name="value">Piped value from object path or previous pipe function.</param>
    /// <param name="arg1">First argument from pipe.</param>
    /// <param name="arg2">Second argument from pipe.</param>
    /// <param name="arg3">Third argument from pipe.</param>
    /// <returns>Result of the pipe function.</returns>
    public delegate EcmaValue Func3(EcmaValue value, EcmaValue arg1, EcmaValue arg2, EcmaValue arg3);

    /// <summary>
    /// Encapsulates a native method that takes three arguments from pipe.
    /// </summary>
    /// <param name="value">Piped value from object path or previous pipe function.</param>
    /// <param name="arg1">First argument from pipe.</param>
    /// <param name="arg2">Second argument from pipe.</param>
    /// <param name="arg3">Third argument from pipe.</param>
    /// <param name="arg4">Forth argument from pipe.</param>
    /// <returns>Result of the pipe function.</returns>
    public delegate EcmaValue Func4(EcmaValue value, EcmaValue arg1, EcmaValue arg2, EcmaValue arg3, EcmaValue arg4);

    /// <summary>
    /// Encapsulates a native method that takes three arguments from pipe.
    /// </summary>
    /// <param name="value">Piped value from object path or previous pipe function.</param>
    /// <param name="arg1">First argument from pipe.</param>
    /// <param name="arg2">Second argument from pipe.</param>
    /// <param name="arg3">Third argument from pipe.</param>
    /// <param name="arg4">Forth argument from pipe.</param>
    /// <param name="arg5">Fifth argument from pipe.</param>
    /// <returns>Result of the pipe function.</returns>
    public delegate EcmaValue Func5(EcmaValue value, EcmaValue arg1, EcmaValue arg2, EcmaValue arg3, EcmaValue arg4, EcmaValue arg5);

    /// <summary>
    /// Invokes the pipe function with the specified context.
    /// </summary>
    /// <param name="context">Pipe context.</param>
    /// <returns>Result of the pipe function.</returns>
    public abstract EcmaValue Invoke(PipeContext context);

    /// <summary>
    /// Creates a pipe function that takes arbitrary number of arguments or function arguments.
    /// </summary>
    /// <param name="fn">A delegate that encapsulates a method that match the signature of <see cref="Variadic"/>.</param>
    /// <returns>The constructed pipe function that can be invoked with a given pipe context.</returns>
    public static PipeFunction Create(Variadic fn) {
      Guard.ArgumentNotNull(fn, "fn");
      return new VariadicPipeFunction(fn);
    }

    /// <summary>
    /// Creates a pipe function that takes no arguments from pipe.
    /// </summary>
    /// <param name="fn">A delegate that encapsulates a method that match the signature of <see cref="Func0"/>.</param>
    /// <returns>The constructed pipe function that can be invoked with a given pipe context.</returns>
    public static PipeFunction Create(Func0 fn) {
      Guard.ArgumentNotNull(fn, "fn");
      return new Func0PipeFunction(fn);
    }

    /// <summary>
    /// Creates a pipe function that takes one argument from pipe.
    /// </summary>
    /// <param name="fn">A delegate that encapsulates a method that match the signature of <see cref="Func1"/>.</param>
    /// <returns>The constructed pipe function that can be invoked with a given pipe context.</returns>
    public static PipeFunction Create(Func1 fn) {
      Guard.ArgumentNotNull(fn, "fn");
      return new Func1PipeFunction(fn);
    }

    /// <summary>
    /// Creates a pipe function that takes two arguments from pipe.
    /// </summary>
    /// <param name="fn">A delegate that encapsulates a method that match the signature of <see cref="Func2"/>.</param>
    /// <returns>The constructed pipe function that can be invoked with a given pipe context.</returns>
    public static PipeFunction Create(Func2 fn) {
      Guard.ArgumentNotNull(fn, "fn");
      return new Func2PipeFunction(fn);
    }

    /// <summary>
    /// Creates a pipe function that takes three arguments from pipe.
    /// </summary>
    /// <param name="fn">A delegate that encapsulates a method that match the signature of <see cref="Func3"/>.</param>
    /// <returns>The constructed pipe function that can be invoked with a given pipe context.</returns>
    public static PipeFunction Create(Func3 fn) {
      Guard.ArgumentNotNull(fn, "fn");
      return new Func3PipeFunction(fn);
    }

    /// <summary>
    /// Creates a pipe function that takes four arguments from pipe.
    /// </summary>
    /// <param name="fn">A delegate that encapsulates a method that match the signature of <see cref="Func4"/>.</param>
    /// <returns>The constructed pipe function that can be invoked with a given pipe context.</returns>
    public static PipeFunction Create(Func4 fn) {
      Guard.ArgumentNotNull(fn, "fn");
      return new Func4PipeFunction(fn);
    }

    /// <summary>
    /// Creates a pipe function that takes five arguments from pipe.
    /// </summary>
    /// <param name="fn">A delegate that encapsulates a method that match the signature of <see cref="Func5"/>.</param>
    /// <returns>The constructed pipe function that can be invoked with a given pipe context.</returns>
    public static PipeFunction Create(Func5 fn) {
      Guard.ArgumentNotNull(fn, "fn");
      return new Func5PipeFunction(fn);
    }

    /// <summary>
    /// Creates a pipe function.
    /// </summary>
    /// <param name="fn">A <see cref="MethodInfo"/> instance representing a named or anonymous method.</param>
    /// <returns>The constructed pipe function that can be invoked with a given pipe context.</returns>
    public static PipeFunction Create(MethodInfo fn) {
      Guard.ArgumentNotNull(fn, "fn");
      ParameterInfo[] p = fn.GetParameters();
      if (fn.ReturnType == typeof(EcmaValue)) {
        if (p.Length == 1 && p[0].ParameterType == typeof(PipeContext)) {
          return Create(CreateDelegate<Variadic>(fn));
        }
        if (p.All(v => v.ParameterType == typeof(EcmaValue))) {
          switch (p.Length) {
            case 1:
              return Create(CreateDelegate<Func0>(fn));
            case 2:
              return Create(CreateDelegate<Func1>(fn));
            case 3:
              return Create(CreateDelegate<Func2>(fn));
            case 4:
              return Create(CreateDelegate<Func3>(fn));
            case 5:
              return Create(CreateDelegate<Func4>(fn));
            case 6:
              return Create(CreateDelegate<Func5>(fn));
          }
        }
      }
      throw new NotSupportedException(String.Format("Unsupported pipe function type {0}", fn));
    }

    /// <summary>
    /// Creates a pipe function that evaluates the specified template on the piped value.
    /// </summary>
    /// <param name="template">A string that represents a valid waterpipe template.</param>
    /// <returns>The constructed pipe function that can be invoked with a given pipe context.</returns>
    public static PipeFunction Create(string template) {
      Guard.ArgumentNotNull(template, "template");
      return new EvaluatedPipeFunction(template);
    }

    private static T CreateDelegate<T>(MethodInfo fn) {
      return (T)(object)Delegate.CreateDelegate(typeof(T), fn);
    }

    #region Implemented classes
    private class EvaluatedPipeFunction : PipeFunction {
      private readonly TokenList tokens;

      public EvaluatedPipeFunction(string template) {
        this.tokens = TokenList.FromString(template);
      }

      public override EcmaValue Invoke(PipeContext context) {
        PipeExecutionException[] exceptions;
        object result = EvaluationContext.Evaluate(tokens, context.Value, new EvaluateOptions(context.EvaluationContext.Options, context.Globals), out exceptions);
        foreach (PipeExecutionException ex in exceptions) {
          context.EvaluationContext.AddException(ex);
        }
        if (context.EvaluationContext.Options.OutputXml) {
          context.EvaluationContext.CurrentXmlElement.AppendChild((XmlNode)result);
          return EcmaValue.Undefined;
        }
        return result.ToString();
      }
    }

    private class VariadicPipeFunction : PipeFunction {
      private readonly Variadic fn;

      public VariadicPipeFunction(Variadic fn) {
        this.fn = fn;
      }

      public override EcmaValue Invoke(PipeContext context) {
        return fn(context);
      }
    }

    private class Func0PipeFunction : PipeFunction {
      private readonly Func0 fn;

      public Func0PipeFunction(Func0 fn) {
        this.fn = fn;
      }

      public override EcmaValue Invoke(PipeContext context) {
        return fn(context.Value);
      }
    }

    private class Func1PipeFunction : PipeFunction {
      private readonly Func1 fn;

      public Func1PipeFunction(Func1 fn) {
        this.fn = fn;
      }

      public override EcmaValue Invoke(PipeContext context) {
        return fn(context.Value, context.TakeArgument());
      }
    }

    private class Func2PipeFunction : PipeFunction {
      private readonly Func2 fn;

      public Func2PipeFunction(Func2 fn) {
        this.fn = fn;
      }

      public override EcmaValue Invoke(PipeContext context) {
        return fn(context.Value, context.TakeArgument(), context.TakeArgument());
      }
    }

    private class Func3PipeFunction : PipeFunction {
      private readonly Func3 fn;

      public Func3PipeFunction(Func3 fn) {
        this.fn = fn;
      }

      public override EcmaValue Invoke(PipeContext context) {
        return fn(context.Value, context.TakeArgument(), context.TakeArgument(), context.TakeArgument());
      }
    }

    private class Func4PipeFunction : PipeFunction {
      private readonly Func4 fn;

      public Func4PipeFunction(Func4 fn) {
        this.fn = fn;
      }

      public override EcmaValue Invoke(PipeContext context) {
        return fn(context.Value, context.TakeArgument(), context.TakeArgument(), context.TakeArgument(), context.TakeArgument());
      }
    }

    private class Func5PipeFunction : PipeFunction {
      private readonly Func5 fn;

      public Func5PipeFunction(Func5 fn) {
        this.fn = fn;
      }

      public override EcmaValue Invoke(PipeContext context) {
        return fn(context.Value, context.TakeArgument(), context.TakeArgument(), context.TakeArgument(), context.TakeArgument(), context.TakeArgument());
      }
    }
    #endregion
  }
}
