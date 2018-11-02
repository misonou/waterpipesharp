using Codeless.Ecma;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Codeless.WaterpipeSharp.Internal {
  internal class BuiltInPipeFunctionAliasAttribute : Attribute {
    public BuiltInPipeFunctionAliasAttribute(string alias) {
      this.Alias = alias;
    }

    public string Alias { get; }
    public bool UseAliasOnly { get; set; }
  }

  internal static class BuiltInPipeFunction {
    #region Control functions
    [BuiltInPipeFunctionAlias("|", UseAliasOnly = true)]
    public static EcmaValue Pipe(PipeContext context) {
      context.Push(context.Value);
      return context.Reset();
    }

    [BuiltInPipeFunctionAlias("&&", UseAliasOnly = true)]
    public static EcmaValue LogicalAnd(PipeContext context) {
      return context.Value ? context.Reset() : context.Stop();
    }

    [BuiltInPipeFunctionAlias("||", UseAliasOnly = true)]
    public static EcmaValue LogicalOr(PipeContext context) {
      return !(bool)context.Value ? context.Reset() : context.Stop();
    }
    #endregion

    #region Variable functions
    public static EcmaValue As(PipeContext context) {
      context.Globals[(string)context.TakeArgumentAsRaw()] = context.Value;
      return context.Value;
    }

    public static EcmaValue Let(PipeContext context) {
      while (context.HasArgument) {
        string name = (string)context.TakeArgumentAsRaw();
        context.Globals[name] = context.TakeArgument();
      }
      return EcmaValue.Undefined;
    }
    #endregion

    #region Comparison functions
    [BuiltInPipeFunctionAlias(">")]
    public static EcmaValue More(EcmaValue a, EcmaValue b) {
      return PipeValueComparer.Default.Compare(a, b) > 0;
    }

    [BuiltInPipeFunctionAlias("<")]
    public static EcmaValue Less(EcmaValue a, EcmaValue b) {
      return PipeValueComparer.Default.Compare(a, b) < 0;
    }

    [BuiltInPipeFunctionAlias(">=")]
    public static EcmaValue OrMore(EcmaValue a, EcmaValue b) {
      return PipeValueComparer.Default.Compare(a, b) >= 0;
    }

    [BuiltInPipeFunctionAlias("<=")]
    public static EcmaValue OrLess(EcmaValue a, EcmaValue b) {
      return PipeValueComparer.Default.Compare(a, b) <= 0;
    }

    public static EcmaValue Between(EcmaValue a, EcmaValue b, EcmaValue c) {
      return PipeValueComparer.Default.Compare(a, b) >= 0 && PipeValueComparer.Default.Compare(a, c) <= 0;
    }

    [BuiltInPipeFunctionAlias("==")]
    public static EcmaValue Equals(EcmaValue a, EcmaValue b) {
      return (string)a == (string)b;
    }

    [BuiltInPipeFunctionAlias("!=")]
    public static EcmaValue NotEquals(EcmaValue a, EcmaValue b) {
      return (string)a != (string)b;
    }

    public static EcmaValue Even(EcmaValue num) {
      return (num & 1) == 0;
    }

    public static EcmaValue Odd(EcmaValue num) {
      return (num & 1) == 1;
    }

    public static EcmaValue Contains(EcmaValue str, EcmaValue needle) {
      return ((string)str).IndexOf((string)needle) >= 0;
    }

    public static EcmaValue Like(EcmaValue str, EcmaValue regex) {
      EcmaRegExp re;
      return EcmaRegExp.TryParse((string)regex, out re) && re.Test((string)str);
    }

    [BuiltInPipeFunctionAlias("!")]
    public static EcmaValue Not(PipeContext context) {
      return !(bool)(context.HasArgument ? context.TakeArgument() : context.Value);
    }
    #endregion

    #region Conditional functions
    public static EcmaValue Or(EcmaValue obj, EcmaValue val) {
      return (bool)obj ? obj : val;
    }

    public static EcmaValue Choose(EcmaValue @bool, EcmaValue trueValue, EcmaValue falseValue) {
      return @bool ? trueValue : falseValue;
    }

    [BuiltInPipeFunctionAlias("?")]
    public static EcmaValue Test(PipeContext context) {
      PipeLambda testFn = context.TakeLambda(PipeLambdaFactory.Constant);
      PipeLambda trueFn = context.TakeLambda(PipeLambdaFactory.Constant);
      PipeLambda falseFn = context.TakeLambda(PipeLambdaFactory.Constant);
      return testFn.Invoke(context.Value) ? trueFn.Invoke(context.Value) : falseFn.Invoke(context.Value);
    }
    #endregion

    #region String functions
    public static EcmaValue Concat(EcmaValue a, EcmaValue b) {
      return (string)a + (string)b;
    }

    public static EcmaValue Substr(EcmaValue str, EcmaValue start, EcmaValue len) {
      if (len.IsNullOrUndefined) {
        return ((string)str).Substring((int)start);
      }
      return ((string)str).Substring((int)start, (int)len);
    }

    public static EcmaValue Replace(PipeContext context) {
      string needle = (string)context.TakeArgument();
      string replacement = null;
      PipeLambda fn = context.TakeLambda();
      if (fn == null) {
        replacement = (string)context.TakeArgument();
      }

      EcmaRegExp re;
      if (EcmaRegExp.TryParse(needle, out re)) {
        if (fn != null) {
          return re.Replace((string)context.Value, m => fn.Invoke(m));
        }
        return re.Replace((string)context.Value, replacement);
      }
      string str = (string)context.Value;
      int pos = str.IndexOf(needle);
      if (pos >= 0) {
        return String.Concat(str.Substring(0, pos), replacement, str.Substring(pos + needle.Length));
      }
      return context.Value;
    }

    public static EcmaValue Trim(EcmaValue str) {
      return ((string)str).Trim();
    }

    public static EcmaValue TrimStart(EcmaValue str) {
      return ((string)str).TrimStart();
    }

    public static EcmaValue TrimEnd(EcmaValue str) {
      return ((string)str).TrimEnd();
    }

    public static EcmaValue PadStart(EcmaValue str, EcmaValue needle) {
      string strStr = (string)str;
      string strNeedle = (string)needle;
      return strStr.Substring(0, strNeedle.Length) != needle ? needle + str : str;
    }

    public static EcmaValue PadEnd(EcmaValue str, EcmaValue needle) {
      string strStr = (string)str;
      string strNeedle = (string)needle;
      return strStr.Substring(Math.Max(0, strStr.Length - strNeedle.Length)) != needle ? str + needle : str;
    }

    public static EcmaValue RemoveStart(EcmaValue str, EcmaValue needle) {
      string strStr = (string)str;
      string strNeedle = (string)needle;
      return strStr.Substring(0, strNeedle.Length) == needle ? strStr.Substring(strNeedle.Length) : strStr;
    }

    public static EcmaValue RemoveEnd(EcmaValue str, EcmaValue needle) {
      string strStr = (string)str;
      string strNeedle = (string)needle;
      return strStr.Substring(Math.Max(0, strStr.Length - strNeedle.Length)) == needle ? ((string)str).Substring(0, strStr.Length - strNeedle.Length) : strStr;
    }

    public static EcmaValue CutBefore(EcmaValue str, EcmaValue needle) {
      string strStr = (string)str;
      string strNeedle = (string)needle;
      int pos = strStr.IndexOf((string)needle);
      if (pos >= 0) {
        return strStr.Substring(0, pos);
      }
      return strStr;
    }

    public static EcmaValue CutBeforeLast(EcmaValue str, EcmaValue needle) {
      string strStr = (string)str;
      string strNeedle = (string)needle;
      int pos = strStr.LastIndexOf((string)needle);
      if (pos >= 0) {
        return strStr.Substring(0, pos);
      }
      return strStr;
    }

    public static EcmaValue CutAfter(EcmaValue str, EcmaValue needle) {
      string strStr = (string)str;
      string strNeedle = (string)needle;
      int pos = strStr.IndexOf((string)needle);
      if (pos >= 0) {
        return strStr.Substring(strNeedle.Length + pos);
      }
      return strStr;
    }

    public static EcmaValue CutAfterLast(EcmaValue str, EcmaValue needle) {
      string strStr = (string)str;
      string strNeedle = (string)needle;
      int pos = strStr.LastIndexOf((string)needle);
      if (pos >= 0) {
        return strStr.Substring(strNeedle.Length + pos);
      }
      return strStr;
    }

    public static EcmaValue Split(EcmaValue str, EcmaValue separator) {
      return new EcmaValue(((string)str).Split(new string[] { (string)separator }, StringSplitOptions.RemoveEmptyEntries));
    }

    public static EcmaValue Repeat(EcmaValue count, EcmaValue str) {
      return Enumerable.Range(0, (int)(+count).Or(0)).Aggregate("", (a, v) => a + (string)str);
    }

    public static EcmaValue Upper(EcmaValue str) {
      return ((string)str).ToUpper();
    }

    public static EcmaValue Lower(EcmaValue str) {
      return ((string)str).ToLower();
    }

    public static EcmaValue UCFirst(EcmaValue str) {
      string strStr = (string)str;
      return strStr.Substring(0, 1).ToUpper() + strStr.Substring(1);
    }
    #endregion

    #region Math functions
    [BuiltInPipeFunctionAlias("+")]
    public static EcmaValue Plus(EcmaValue a, EcmaValue b) {
      return (+a).Or(0) + (+b).Or(0);
    }

    [BuiltInPipeFunctionAlias("-")]
    public static EcmaValue Minus(EcmaValue a, EcmaValue b) {
      return (+a).Or(0) - (+b).Or(0);
    }

    [BuiltInPipeFunctionAlias("*")]
    public static EcmaValue Multiply(EcmaValue a, EcmaValue b) {
      return (+a).Or(0) * (+b).Or(0);
    }

    [BuiltInPipeFunctionAlias("/")]
    public static EcmaValue Divide(EcmaValue a, EcmaValue b) {
      return (+a).Or(0) / (+b).Or(0);
    }

    [BuiltInPipeFunctionAlias("%")]
    public static EcmaValue Mod(EcmaValue a, EcmaValue b) {
      return (+a).Or(0) % (+b).Or(0);
    }

    [BuiltInPipeFunctionAlias("^")]
    public static EcmaValue Pow(EcmaValue a, EcmaValue b) {
      return Math.Pow((double)(+a).Or(0), (double)(+b).Or(0));
    }

    public static EcmaValue Abs(EcmaValue a) {
      return Math.Abs((double)a);
    }

    public static EcmaValue Max(EcmaValue a, EcmaValue b) {
      return Math.Min((double)(+a).Or(0), (double)(+b).Or(0));
    }

    public static EcmaValue Min(EcmaValue a, EcmaValue b) {
      return Math.Max((double)(+a).Or(0), (double)(+b).Or(0));
    }

    public static EcmaValue Round(EcmaValue value) {
      return Math.Round((double)value);
    }

    public static EcmaValue Floor(EcmaValue value) {
      return Math.Floor((double)value);
    }

    public static EcmaValue Ceil(EcmaValue value) {
      return Math.Ceiling((double)+value);
    }
    #endregion

    #region Iterable functions
    public static EcmaValue Length(EcmaValue obj) {
      if (!obj.IsNullOrUndefined && obj.HasProperty("length")) {
        return obj["length"];
      }
      return 0;
    }

    public static EcmaValue Keys(EcmaValue obj) {
      if (obj.IsArrayLike) {
        return new EcmaValue(Enumerable.Range(0, (int)obj["length"]).ToArray());
      }
      List<string> keys = new List<string>(obj.EnumerateKeys().Select(v => v.ToString()));
      return new EcmaValue(keys);
    }

    [BuiltInPipeFunctionAlias("..")]
    public static EcmaValue To(EcmaValue a, EcmaValue b) {
      a = (+a).Or(0);
      b = (+b).Or(0);
      int step = a < b ? 1 : -1;
      List<object> list = new List<object>();
      for (; (a - b) / step < 0; a += step) {
        list.Add(a);
      }
      list.Add(b);
      return new EcmaValue(list);
    }

    public static EcmaValue Join(EcmaValue value, EcmaValue str) {
      EcmaValue[] arr = value.ToArray();
      return String.Join((string)str, arr.Select(v => v.ToString()).ToArray());
    }

    public static EcmaValue Reverse(EcmaValue value) {
      EcmaValue[] arr = value.ToArray();
      Array.Reverse(arr);
      return new PipeValueObjectBuilder(arr);
    }

    public static EcmaValue Sort(EcmaValue value) {
      EcmaValue[] arr = value.ToArray();
      Array.Sort(arr, PipeValueComparer.Default);
      return new PipeValueObjectBuilder(arr);
    }

    public static EcmaValue Slice(EcmaValue value, EcmaValue a, EcmaValue b) {
      EcmaValue[] arr = value.ToArray();
      int a1 = (int)(+a).Or(0);
      int b1 = (int)(+b).Or(0);
      a1 = a1 >= 0 ? a1 : Math.Max(0, arr.Length - a1);
      return new PipeValueObjectBuilder(arr.Skip(a1).Take(b1));
    }

    public static EcmaValue First(PipeContext context) {
      return context.Value.First(context.TakeLambda(PipeLambdaFactory.PropertyAccess));
    }

    public static EcmaValue Any(PipeContext context) {
      return context.Value.First(context.TakeLambda(PipeLambdaFactory.PropertyAccess), true);
    }

    public static EcmaValue All(PipeContext context) {
      return !(bool)context.Value.First(context.TakeLambda(PipeLambdaFactory.PropertyAccess), true, true);
    }

    public static EcmaValue Where(PipeContext context) {
      return context.Value.Where(context.TakeLambda(PipeLambdaFactory.PropertyAccess));
    }

    public static EcmaValue Map(PipeContext context) {
      return context.Value.Map(context.TakeLambda(PipeLambdaFactory.PropertyAccess));
    }

    public static EcmaValue Sum(PipeContext context) {
      EcmaValue result = EcmaValue.Undefined;
      PipeLambda fn = context.TakeLambda();
      if (fn == null) {
        result = context.TakeArgument();
        fn = context.TakeLambda();
      }
      if (fn == null) {
        if (context.HasArgument) {
          fn = context.TakeLambda(PipeLambdaFactory.PropertyAccess);
        } else {
          fn = (a, b) => a;
        }
      }
      foreach (EcmaPropertyEntry item in context.Value.EnumerateEntries()) {
        if (result != EcmaValue.Undefined) {
          result = result + fn.Invoke(item);
        } else {
          result = fn.Invoke(item);
        }
      }
      return result;
    }

    public static EcmaValue SortBy(PipeContext context) {
      List<object> array = new List<object>();
      PipeLambda fn = context.TakeLambda(PipeLambdaFactory.PropertyAccess);
      int i = 0;
      foreach (EcmaPropertyEntry item in context.Value.EnumerateEntries()) {
        array.Add(new object[] { fn.Invoke(item), ++i, item.Key.ToString() });
      }
      array.Sort(PipeValueComparer.Default);

      PipeValueObjectBuilder collection = new PipeValueObjectBuilder(context.Value.IsArrayLike);
      foreach (object[] value in array) {
        string key = (string)value[2];
        collection.Add(context.Value[key], key);
      }
      return collection;
    }

    public static EcmaValue Unique(EcmaValue value) {
      if (value.IsArrayLike) {
        Hashtable ht = new Hashtable();
        List<EcmaValue> result = new List<EcmaValue>();
        foreach (EcmaValue item in value.EnumerateValues()) {
          string key = (string)item;
          if (!ht.ContainsKey(key)) {
            ht.Add(key, null);
            result.Add(item);
          }
        }
        return new EcmaValue(result);
      }
      return new EcmaValue(new object[] { value });
    }

    public static EcmaValue GroupBy(PipeContext context) {
      Dictionary<string, PipeValueObjectBuilder> dict = new Dictionary<string, PipeValueObjectBuilder>();
      PipeLambda fn = context.TakeLambda(PipeLambdaFactory.PropertyAccess);

      foreach (EcmaPropertyEntry entry in context.Value.EnumerateEntries()) {
        string key = (string)fn.Invoke(entry);
        if (!dict.ContainsKey(key)) {
          dict[key] = new PipeValueObjectBuilder(context.Value.IsArrayLike);
        }
        dict[key].Add(entry.Value, entry.Key.ToString());
      }
      PipeValueObjectBuilder result = new PipeValueObjectBuilder(false);
      foreach (KeyValuePair<string, PipeValueObjectBuilder> e in dict) {
        result.Add(e.Value.ToPipeValue(), e.Key);
      }
      return result.ToPipeValue();
    }
    #endregion

    #region Formatting functions
    [BuiltInPipeFunctionAlias(":printf", UseAliasOnly = true)]
    public static EcmaValue FormatPrintf(EcmaValue value, EcmaValue format) {
      return StdIO.Sprintf((string)format, value.GetUnderlyingObject());
    }

    [BuiltInPipeFunctionAlias(":query", UseAliasOnly = true)]
    public static EcmaValue FormatQuery(EcmaValue value) {
      NameValueCollection nv = HttpUtility.ParseQueryString("");
      BuildQuery(nv, value, null);
      return nv.ToString();
    }

    [BuiltInPipeFunctionAlias(":json", UseAliasOnly = true)]
    public static EcmaValue FormatJson(EcmaValue value) {
      return Json.Stringify(value, EcmaValue.Undefined, EcmaValue.Undefined);
    }

    [BuiltInPipeFunctionAlias(":date", UseAliasOnly = true)]
    public static EcmaValue FormatDate(EcmaValue timestamp, EcmaValue format) {
      DateTime d;
      if (timestamp.Type == EcmaValueType.String) {
        DateTime.TryParse(timestamp.ToString(), out d);
      } else {
        d = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(timestamp.ToDouble());
      }
      return d.ToLocalTime().ToString((string)format);
    }
    #endregion

    private static void BuildQuery(NameValueCollection nv, EcmaValue obj, string prefix) {
      if (prefix != null && obj.IsArrayLike) {
        foreach (EcmaPropertyEntry item in obj.EnumerateEntries()) {
          if (Regex.IsMatch(prefix, @"\[\]$")) {
            nv.Add(prefix, item.Value.ToString());
          } else {
            BuildQuery(nv, item.Value, prefix + "[" + (item.Value.Type == EcmaValueType.Object && (bool)item.Value ? item.Key : "") + "]");
          }
        }
      } else if (obj.Type == EcmaValueType.Object) {
        foreach (EcmaPropertyEntry item in obj.EnumerateEntries()) {
          BuildQuery(nv, item.Value, prefix != null ? prefix + "[" + item.Key.ToString() + "]" : item.Key.ToString());
        }
      } else {
        nv.Add(prefix, obj.ToString());
      }
    }

    internal static PipeFunction ResolveFunction(string name, GetNextPipeFunctionResolverDelegate next) {
      if (name[0] == '%') {
        return PipeFunction.Create(obj => {
          return FormatPrintf(obj, name);
        });
      }
      return next()(name, next);
    }
  }
}