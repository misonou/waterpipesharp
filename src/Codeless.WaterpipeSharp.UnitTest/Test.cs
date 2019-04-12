using Codeless.Ecma;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Codeless.WaterpipeSharp.UnitTest {
  [TestFixture]
  public class Test {
    public static readonly PipeGlobal globals = new PipeGlobal();
    public static readonly List<TestCaseData> tests = new List<TestCaseData>();

    static Test() {
      Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
      IDictionary spec = (IDictionary)ToSimpleObjectGraph((JToken)JsonConvert.DeserializeObject(File.ReadAllText("test.json")));
      foreach (DictionaryEntry e in (IDictionary)spec["globals"]) {
        globals[(string)e.Key] = new EcmaValue(e.Value);
      }
      foreach (DictionaryEntry e in (IDictionary)spec["pipes"]) {
        Waterpipe.RegisterFunction((string)e.Key, (string)e.Value);
      }
      Waterpipe.RegisterFunction("fn1", (EcmaValue a) => 1);
      Waterpipe.RegisterFunction("fn2", (EcmaValue a, EcmaValue b) => 2);
      Waterpipe.RegisterFunction("fn3", (EcmaValue a, EcmaValue b, EcmaValue c) => 3);
      Waterpipe.RegisterFunction("fn4", (EcmaValue a, EcmaValue b, EcmaValue c, EcmaValue d) => 4);
      Waterpipe.RegisterFunction("fn5", (EcmaValue a, EcmaValue b, EcmaValue c, EcmaValue d, EcmaValue e) => 5);

      foreach (DictionaryEntry e in (IDictionary)spec["tests"]) {
        foreach (DictionaryEntry f in (IDictionary)e.Value) {
          IDictionary obj = (IDictionary)f.Value;
          TestCaseData test = new TestCaseData(obj["input"], obj["template"] ?? "", obj["expect"], obj["globals"], obj["exception"], obj["func"]);
          test.SetName("<" + (string)e.Key + ": " + (string)f.Key + ">");
          tests.Add(test);
        }
      }
    }

    [Test, TestCaseSource("tests")]
    public void Run(object input, string template, object expected, IDictionary myGlobals, bool? exception, string func) {
      EvaluateOptions options = new EvaluateOptions();
      if (myGlobals != null) {
        options.Globals = new PipeGlobal(myGlobals);
      } else {
        options.Globals = globals;
      }
      Func<string, object, EvaluateOptions, object> execute = func == "eval" ?
        (Func<string, object, EvaluateOptions, object>)Waterpipe.EvaluateSingle :
        (Func<string, object, EvaluateOptions, object>)Waterpipe.Evaluate;
      if (exception.HasValue && exception.Value) {
        Assert.Throws(Is.InstanceOf<WaterpipeException>(), () => execute(template, input, options));
      } else {
        object actual = execute(template, input, options);
        Assert.AreEqual(ToComparableResult(expected), ToComparableResult(actual));
      }
    }

    private static string ToComparableResult(object value) {
      if (value == null || value is string) {
        return (string)value;
      }
      return Json.Stringify(new EcmaValue(value));
    }

    private static object ToSimpleObjectGraph(JToken value) {
      switch (value.Type) {
        case JTokenType.Object:
          return ((JObject)value).Properties().ToDictionary(v => v.Name, v => ToSimpleObjectGraph(v.Value));
        case JTokenType.Array:
          return ((JArray)value).Select(ToSimpleObjectGraph).ToList();
        case JTokenType.String:
          return value.ToString();
        case JTokenType.Boolean:
          return value.ToObject<bool>();
        case JTokenType.Null:
          return null;
        case JTokenType.Integer:
          return value.ToObject<int>();
        case JTokenType.Float:
          return value.ToObject<double>();
      }
      throw new NotSupportedException();
    }
  }
}
