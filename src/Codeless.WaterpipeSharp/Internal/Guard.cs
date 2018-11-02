using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Web;

namespace Codeless {
  [DebuggerStepThrough]
  internal static class Guard {
    public static T ArgumentNotNull<T>(T value, string argumentName) {
      if (Object.ReferenceEquals(value, null)) {
        throw new ArgumentNullException(argumentName);
      }
      return value;
    }
  }
}
