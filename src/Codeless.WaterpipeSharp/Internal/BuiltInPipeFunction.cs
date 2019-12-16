using Codeless.Ecma;
using Codeless.Ecma.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
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
      context.Globals[Helper.String(context.TakeArgumentAsRaw())] = context.Value;
      return context.Value;
    }

    public static EcmaValue Let(PipeContext context) {
      while (context.HasArgument) {
        string name = Helper.String(context.TakeArgumentAsRaw());
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
      return Helper.String(a) == Helper.String(b);
    }

    [BuiltInPipeFunctionAlias("!=")]
    public static EcmaValue NotEquals(EcmaValue a, EcmaValue b) {
      return Helper.String(a) != Helper.String(b);
    }

    [BuiltInPipeFunctionAlias("~=")]
    public static EcmaValue IEquals(EcmaValue a, EcmaValue b) {
      return Helper.String(a).Equals(Helper.String(b), StringComparison.CurrentCultureIgnoreCase);
    }

    [BuiltInPipeFunctionAlias("!~=")]
    public static EcmaValue INotEquals(EcmaValue a, EcmaValue b) {
      return !Helper.String(a).Equals(Helper.String(b), StringComparison.CurrentCultureIgnoreCase);
    }

    [BuiltInPipeFunctionAlias("^=")]
    public static EcmaValue StartsWith(EcmaValue a, EcmaValue b) {
      string needle = Helper.String(b);
      return !String.IsNullOrEmpty(needle) && Helper.String(a).StartsWith(needle, StringComparison.Ordinal);
    }

    [BuiltInPipeFunctionAlias("$=")]
    public static EcmaValue EndsWith(EcmaValue a, EcmaValue b) {
      string needle = Helper.String(b);
      return !String.IsNullOrEmpty(needle) && Helper.String(a).EndsWith(needle, StringComparison.Ordinal);
    }

    public static EcmaValue Even(EcmaValue num) {
      return (num & 1) == 0;
    }

    public static EcmaValue Odd(EcmaValue num) {
      return (num & 1) == 1;
    }

    [BuiltInPipeFunctionAlias("*=")]
    public static EcmaValue Contains(EcmaValue str, EcmaValue needle) {
      return Helper.String(str).IndexOf(Helper.String(needle)) >= 0;
    }

    public static EcmaValue Like(EcmaValue str, EcmaValue regex) {
      EcmaRegExp re = Helper.ParseRegExp(Helper.String(regex));
      return re != null ? re.Test(Helper.String(str)) : false;
    }

    [BuiltInPipeFunctionAlias("!")]
    public static EcmaValue Not(PipeContext context) {
      return !(bool)(context.HasArgument ? context.TakeArgument() : context.Value);
    }
    #endregion

    #region Conditional functions
    public static EcmaValue Or(EcmaValue obj, EcmaValue val) {
      return obj ? obj : val;
    }

    [BuiltInPipeFunctionAlias("??", UseAliasOnly = true)]
    public static EcmaValue Coalesce(EcmaValue obj, EcmaValue val) {
      return obj.IsNullOrUndefined ? val : obj;
    }

    public static EcmaValue Choose(EcmaValue @bool, EcmaValue trueValue, EcmaValue falseValue) {
      return @bool ? trueValue : falseValue;
    }

    [BuiltInPipeFunctionAlias("?")]
    public static EcmaValue Test(PipeContext context) {
      PipeLambda testFn = context.TakeLambda(PipeLambdaFactory.Constant);
      PipeLambda trueFn = context.TakeLambda(PipeLambdaFactory.Constant);
      PipeLambda falseFn = context.TakeLambda(PipeLambdaFactory.Constant);
      return testFn.Invoke(context, context.Value) ? trueFn.Invoke(context, context.Value) : falseFn.Invoke(context, context.Value);
    }
    #endregion

    #region String functions
    public static EcmaValue Concat(EcmaValue a, EcmaValue b) {
      return Helper.String(a) + Helper.String(b);
    }

    public static EcmaValue Substr(EcmaValue str, EcmaValue start, EcmaValue len) {
      if (len.IsNullOrUndefined) {
        return Helper.String(str).Substring((int)start);
      }
      return Helper.String(str).Substring((int)start, (int)len);
    }

    public static EcmaValue Replace(PipeContext context) {
      string needle = Helper.String(context.TakeArgument());
      string replacement = null;
      PipeLambda fn = context.TakeLambda();
      if (fn == null) {
        replacement = Helper.String(context.TakeArgument());
      }
      EcmaRegExp re = Helper.ParseRegExp(needle);
      if (re != null) {
        if (fn != null) {
          return re.Replace(Helper.String(context.Value), m => fn.Invoke(context, m.ToValue()).ToString());
        }
        return re.Replace(Helper.String(context.Value), replacement);
      }
      string str = Helper.String(context.Value);
      int pos = str.IndexOf(needle, StringComparison.Ordinal);
      if (pos >= 0) {
        return String.Concat(str.Substring(0, pos), replacement, str.Substring(pos + needle.Length));
      }
      return context.Value;
    }

    public static EcmaValue Trim(EcmaValue str) {
      return Helper.String(str).Trim();
    }

    public static EcmaValue TrimStart(EcmaValue str) {
      return Helper.String(str).TrimStart();
    }

    public static EcmaValue TrimEnd(EcmaValue str) {
      return Helper.String(str).TrimEnd();
    }

    public static EcmaValue PadStart(EcmaValue str, EcmaValue needle) {
      string strStr = Helper.String(str);
      string strNeedle = Helper.String(needle);
      return strStr.Substring(0, strNeedle.Length) != needle ? needle + str : str;
    }

    public static EcmaValue PadEnd(EcmaValue str, EcmaValue needle) {
      string strStr = Helper.String(str);
      string strNeedle = Helper.String(needle);
      return strStr.Substring(Math.Max(0, strStr.Length - strNeedle.Length)) != needle ? str + needle : str;
    }

    public static EcmaValue RemoveStart(EcmaValue str, EcmaValue needle) {
      string strStr = Helper.String(str);
      string strNeedle = Helper.String(needle);
      return strStr.Substring(0, strNeedle.Length) == needle ? strStr.Substring(strNeedle.Length) : strStr;
    }

    public static EcmaValue RemoveEnd(EcmaValue str, EcmaValue needle) {
      string strStr = Helper.String(str);
      string strNeedle = Helper.String(needle);
      return strStr.Substring(Math.Max(0, strStr.Length - strNeedle.Length)) == needle ? Helper.String(str).Substring(0, strStr.Length - strNeedle.Length) : strStr;
    }

    public static EcmaValue CutBefore(EcmaValue str, EcmaValue needle) {
      string strStr = Helper.String(str);
      string strNeedle = Helper.String(needle);
      int pos = strStr.IndexOf(Helper.String(needle), StringComparison.Ordinal);
      if (pos >= 0) {
        return strStr.Substring(0, pos);
      }
      return strStr;
    }

    public static EcmaValue CutBeforeLast(EcmaValue str, EcmaValue needle) {
      string strStr = Helper.String(str);
      string strNeedle = Helper.String(needle);
      int pos = strStr.LastIndexOf(Helper.String(needle), StringComparison.Ordinal);
      if (pos >= 0) {
        return strStr.Substring(0, pos);
      }
      return strStr;
    }

    public static EcmaValue CutAfter(EcmaValue str, EcmaValue needle) {
      string strStr = Helper.String(str);
      string strNeedle = Helper.String(needle);
      int pos = strStr.IndexOf(Helper.String(needle), StringComparison.Ordinal);
      if (pos >= 0) {
        return strStr.Substring(strNeedle.Length + pos);
      }
      return strStr;
    }

    public static EcmaValue CutAfterLast(EcmaValue str, EcmaValue needle) {
      string strStr = Helper.String(str);
      string strNeedle = Helper.String(needle);
      int pos = strStr.LastIndexOf(Helper.String(needle), StringComparison.Ordinal);
      if (pos >= 0) {
        return strStr.Substring(strNeedle.Length + pos);
      }
      return strStr;
    }

    public static EcmaValue Split(EcmaValue str, EcmaValue separator) {
      return new EcmaValue(Helper.String(str).Split(new string[] { Helper.String(separator) }, StringSplitOptions.RemoveEmptyEntries));
    }

    public static EcmaValue Repeat(EcmaValue count, EcmaValue str) {
      string strStr = Helper.String(str);
      int intCount = (int)(+count || (EcmaValue)0);
      StringBuilder sb = new StringBuilder(strStr.Length * intCount);
      for (int i = 0; i < intCount; i++) {
        sb.Append(strStr);
      }
      return sb.ToString();
    }

    public static EcmaValue Upper(EcmaValue str) {
      return Helper.String(str).ToUpper();
    }

    public static EcmaValue Lower(EcmaValue str) {
      return Helper.String(str).ToLower();
    }

    public static EcmaValue UCFirst(EcmaValue str) {
      string strStr = Helper.String(str);
      return strStr.Substring(0, 1).ToUpper() + strStr.Substring(1);
    }

    public static EcmaValue LCFirst(EcmaValue str) {
      string strStr = Helper.String(str);
      return strStr.Substring(0, 1).ToLower() + strStr.Substring(1);
    }

    public static EcmaValue Hyphenate(EcmaValue str) {
      return Regex.Replace(Helper.String(str), "[A-Z](?:[A-Z]+(?=$|[A-Z]))?", m => {
        return (m.Index > 0 ? "-" : "") + m.Value.ToLower();
      });
    }
    #endregion

    #region Math functions
    [BuiltInPipeFunctionAlias("+")]
    public static EcmaValue Plus(EcmaValue a, EcmaValue b) {
      return (+a || (EcmaValue)0) + (+b || (EcmaValue)0);
    }

    [BuiltInPipeFunctionAlias("-")]
    public static EcmaValue Minus(EcmaValue a, EcmaValue b) {
      return (+a || (EcmaValue)0) - (+b || (EcmaValue)0);
    }

    [BuiltInPipeFunctionAlias("*")]
    public static EcmaValue Multiply(EcmaValue a, EcmaValue b) {
      return (+a || (EcmaValue)0) * (+b || (EcmaValue)0);
    }

    [BuiltInPipeFunctionAlias("/")]
    public static EcmaValue Divide(EcmaValue a, EcmaValue b) {
      return (+a || (EcmaValue)0) / (+b || (EcmaValue)0);
    }

    [BuiltInPipeFunctionAlias("%")]
    public static EcmaValue Mod(EcmaValue a, EcmaValue b) {
      return (+a || (EcmaValue)0) % (+b || (EcmaValue)0);
    }

    [BuiltInPipeFunctionAlias("^")]
    public static EcmaValue Pow(EcmaValue a, EcmaValue b) {
      return EcmaMath.Pow(+a || (EcmaValue)0, +b || (EcmaValue)0);
    }

    public static EcmaValue Abs(EcmaValue a) {
      return Math.Abs((double)a);
    }

    public static EcmaValue Max(EcmaValue a, EcmaValue b) {
      return EcmaMath.Min(+a || (EcmaValue)0, +b || (EcmaValue)0);
    }

    public static EcmaValue Min(EcmaValue a, EcmaValue b) {
      return EcmaMath.Max(+a || (EcmaValue)0, +b || (EcmaValue)0);
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
      if (obj.IsNullOrUndefined) {
        return new EcmaValue(new EcmaArray());
      }
      if (obj.IsArrayLike) {
        return new EcmaValue(Enumerable.Range(0, (int)obj["length"]).ToArray());
      }
      List<string> keys = new List<string>(obj.ToObject().GetEnumerablePropertyKeys().Select(v => v.ToString()));
      return new EcmaValue(keys);
    }

    [BuiltInPipeFunctionAlias("..")]
    public static EcmaValue To(EcmaValue a, EcmaValue b) {
      a = +a || (EcmaValue)0;
      b = +b || (EcmaValue)0;
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
      return String.Join(Helper.String(str), arr.Select(v => v.ToString()).ToArray());
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
      int a1 = (int)(+a || (EcmaValue)0);
      int b1 = (int)(+b || (EcmaValue)0);
      a1 = a1 >= 0 ? a1 : Math.Max(0, arr.Length - a1);
      return new PipeValueObjectBuilder(arr.Skip(a1).Take(b1));
    }

    public static EcmaValue First(PipeContext context) {
      return context.Value.First(context, Helper.DetectKeyLambda(context, context.Value));
    }

    public static EcmaValue Any(PipeContext context) {
      return context.Value.First(context, Helper.DetectKeyLambda(context, context.Value), true);
    }

    public static EcmaValue All(PipeContext context) {
      return !(bool)context.Value.First(context, Helper.DetectKeyLambda(context, context.Value), true, true);
    }

    public static EcmaValue None(PipeContext context) {
      return !(bool)context.Value.First(context, Helper.DetectKeyLambda(context, context.Value), true);
    }

    public static EcmaValue Where(PipeContext context) {
      return context.Value.Where(context, Helper.DetectKeyLambda(context, context.Value));
    }

    public static EcmaValue Map(PipeContext context) {
      return context.Value.Map(context, Helper.DetectKeyLambda(context, context.Value));
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
          fn = Helper.DetectKeyLambda(context, context.Value);
        } else {
          fn = (c, a, b) => a;
        }
      }
      foreach (EcmaPropertyKey propertyKey in context.Value) {
        if (result != EcmaValue.Undefined) {
          result = result + fn.Invoke(context, context.Value[propertyKey], propertyKey.ToValue());
        } else {
          result = fn.Invoke(context, context.Value[propertyKey], propertyKey.ToValue());
        }
      }
      return result;
    }

    public static EcmaValue SortBy(PipeContext context) {
      List<object> array = new List<object>();
      PipeLambda fn = Helper.DetectKeyLambda(context, context.Value);
      int i = 0;
      foreach (EcmaPropertyKey propertyKey in context.Value) {
        array.Add(new object[] { fn.Invoke(context, context.Value[propertyKey], propertyKey.ToValue()), ++i, propertyKey.ToString() });
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
        foreach (EcmaPropertyKey propertyKey in value) {
          string key = Helper.String(value[propertyKey]);
          if (!ht.ContainsKey(key)) {
            ht.Add(key, null);
            result.Add(value[propertyKey]);
          }
        }
        return new EcmaValue(result);
      }
      return new EcmaValue(new object[] { value });
    }

    public static EcmaValue GroupBy(PipeContext context) {
      Dictionary<string, PipeValueObjectBuilder> dict = new Dictionary<string, PipeValueObjectBuilder>();
      PipeLambda fn = Helper.DetectKeyLambda(context, context.Value);

      foreach (EcmaPropertyKey propertyKey in context.Value) {
        string key = Helper.String(fn.Invoke(context, context.Value[propertyKey], propertyKey.ToValue()));
        if (!dict.ContainsKey(key)) {
          dict[key] = new PipeValueObjectBuilder(context.Value.IsArrayLike);
        }
        dict[key].Add(context.Value[propertyKey], propertyKey.ToString());
      }
      PipeValueObjectBuilder result = new PipeValueObjectBuilder(false);
      foreach (KeyValuePair<string, PipeValueObjectBuilder> e in dict) {
        result.Add(e.Value.ToPipeValue(), e.Key);
      }
      return result.ToPipeValue();
    }

    public static EcmaValue In(PipeContext context) {
      EcmaValue b = context.TakeArgument(true);
      return b.IsArrayLike ? b.Invoke("indexOf", context.Value) >= 0 : b.Type == EcmaValueType.Object && b.HasProperty(EcmaPropertyKey.FromValue(context.Value));
    }
    #endregion

    #region Formatting functions
    [BuiltInPipeFunctionAlias(":printf", UseAliasOnly = true)]
    public static EcmaValue FormatPrintf(EcmaValue value, EcmaValue format) {
      return StdIO.Sprintf(Helper.String(format), value.GetUnderlyingObject());
    }

    [BuiltInPipeFunctionAlias(":query", UseAliasOnly = true)]
    public static EcmaValue FormatQuery(EcmaValue value) {
      NameValueCollection nv = HttpUtility.ParseQueryString("");
      BuildQuery(nv, value, null);
      return nv.ToString();
    }

    [BuiltInPipeFunctionAlias(":json", UseAliasOnly = true)]
    public static EcmaValue FormatJson(EcmaValue value) {
      return Helper.Json(value);
    }

    [BuiltInPipeFunctionAlias(":date", UseAliasOnly = true)]
    public static EcmaValue FormatDate(EcmaValue timestamp, EcmaValue format) {
      DateTime d;
      if (!(bool)format) {
        return "";
      }
      if (timestamp.GetUnderlyingObject() is EcmaDate date) {
        d = date.Value;
      } else if (timestamp.Type == EcmaValueType.String) {
        d = new EcmaDate(timestamp.ToString(true)).Value;
      } else {
        d = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(timestamp.ToDouble());
      }
      return d.ToLocalTime().ToString(Helper.String(format), DateTimeFormatInfo.InvariantInfo);
    }
    #endregion

    private static void BuildQuery(NameValueCollection nv, EcmaValue obj, string prefix) {
      if (prefix != null && obj.IsArrayLike) {
        foreach (EcmaPropertyKey propertyKey in obj) {
          if (Regex.IsMatch(prefix, @"\[\]$")) {
            nv.Add(prefix, obj[propertyKey].ToString());
          } else {
            BuildQuery(nv, obj[propertyKey], prefix + "[" + (obj[propertyKey].Type == EcmaValueType.Object && (bool)obj[propertyKey] ? propertyKey.ToString() : "") + "]");
          }
        }
      } else if (obj.Type == EcmaValueType.Object) {
        foreach (EcmaPropertyKey propertyKey in obj) {
          BuildQuery(nv, obj[propertyKey], prefix != null ? prefix + "[" + propertyKey.ToString() + "]" : propertyKey.ToString());
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
