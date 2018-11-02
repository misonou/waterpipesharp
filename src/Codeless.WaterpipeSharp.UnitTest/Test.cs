using Codeless.Ecma;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace Codeless.WaterpipeSharp.UnitTest {
  [TestFixture]
  public class Test {
    public static readonly PipeGlobal globals = new PipeGlobal();
    public static readonly List<TestCaseData> tests = new List<TestCaseData>();

    static Test() {
      IDictionary spec = (IDictionary)new JavaScriptSerializer().DeserializeObject(System.IO.File.ReadAllText("test.json"));
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
          TestCaseData test = new TestCaseData(obj["input"], obj["template"], obj["expect"], obj["globals"], obj["exception"]);
          test.SetName("<" + (string)e.Key + ": " + (string)f.Key + ">");
          tests.Add(test);
        }
      }
    }

    [Test, TestCaseSource("tests")]
    public void Run(object input, string template, string expected, IDictionary myGlobals, bool? exception) {
      EvaluateOptions options = new EvaluateOptions();
      if (myGlobals != null) {
        options.Globals = new PipeGlobal(myGlobals);
      } else {
        options.Globals = globals;
      }
      if (exception.HasValue && exception.Value) {
        Assert.Throws(Is.InstanceOf<WaterpipeException>(), () => Waterpipe.Evaluate(template ?? "", input, options));
      } else {
        string actual = Waterpipe.Evaluate(template ?? "", input, options);
        Assert.AreEqual(expected, actual);
      }
    }
  }
}
