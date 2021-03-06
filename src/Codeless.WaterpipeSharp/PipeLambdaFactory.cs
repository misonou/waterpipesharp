﻿using Codeless.Ecma;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Codeless.WaterpipeSharp {
  public abstract class PipeLambdaFactory {
    public static readonly PipeLambdaFactory Constant = new ConstantPipeLambdaFactory();
    public static readonly PipeLambdaFactory PropertyAccess = new PropertyAccessPipeLambdaFactory();

    public abstract PipeLambda CreateLambda(EcmaValue value);

    private class ConstantPipeLambdaFactory : PipeLambdaFactory {
      public override PipeLambda CreateLambda(EcmaValue value) {
        return (c, a, b) => value;
      }
    }

    private class PropertyAccessPipeLambdaFactory : PipeLambdaFactory {
      public override PipeLambda CreateLambda(EcmaValue value) {
        return (c, a, b) => a[(string)value];
      }
    }
  }
}
