﻿using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Codeless.WaterpipeSharp.Internal {
  // https://www.codeproject.com/Articles/19274/A-printf-implementation-in-C
  internal static class StdIO {
    public static string Sprintf(string format, params object[] parameters) {
      #region Variables
      StringBuilder f = new StringBuilder();
      Regex r = new Regex(@"\%([\'\#\-\+ ]*)(\d*)(?:\.(\d+))?([hl])?([dioxXucsfeEgGpn%])");
      Match m = null;
      string w = String.Empty;
      long i = 0;
      object o = null;

      bool flagLeft2Right = false;
      bool flagAlternate = false;
      bool flagPositiveSign = false;
      bool flagPositiveSpace = false;
      bool flagZeroPadding = false;
      bool flagGroupThousands = false;

      int fieldLength = 0;
      int fieldPrecision = 0;
      char shortLongIndicator = '\0';
      char formatSpecifier = '\0';
      char paddingCharacter = ' ';
      #endregion

      // find all format parameters in format string
      f.Append(format);
      m = r.Match(f.ToString());
      while (m.Success) {
        #region format flags
        // extract format flags
        flagAlternate = false;
        flagLeft2Right = false;
        flagPositiveSign = false;
        flagPositiveSpace = false;
        flagZeroPadding = false;
        flagGroupThousands = false;
        if (m.Groups[1] != null && m.Groups[1].Value.Length > 0) {
          string flags = m.Groups[1].Value;

          flagAlternate = (flags.IndexOf('#') >= 0);
          flagLeft2Right = (flags.IndexOf('-') >= 0);
          flagPositiveSign = (flags.IndexOf('+') >= 0);
          flagPositiveSpace = (flags.IndexOf(' ') >= 0);
          flagGroupThousands = (flags.IndexOf('\'') >= 0);

          // positive + indicator overrides a
          // positive space character
          if (flagPositiveSign && flagPositiveSpace)
            flagPositiveSpace = false;
        }
        #endregion

        #region field length
        // extract field length and 
        // pading character
        paddingCharacter = ' ';
        fieldLength = int.MinValue;
        if (m.Groups[2] != null && m.Groups[2].Value.Length > 0) {
          fieldLength = Convert.ToInt32(m.Groups[2].Value);
          flagZeroPadding = (m.Groups[2].Value[0] == '0');
        }
        #endregion

        if (flagZeroPadding)
          paddingCharacter = '0';

        // left2right allignment overrides zero padding
        if (flagLeft2Right && flagZeroPadding) {
          flagZeroPadding = false;
          paddingCharacter = ' ';
        }

        #region field precision
        // extract field precision
        fieldPrecision = int.MinValue;
        if (m.Groups[3] != null && m.Groups[3].Value.Length > 0)
          fieldPrecision = Convert.ToInt32(m.Groups[3].Value);
        #endregion

        #region short / long indicator
        // extract short / long indicator
        shortLongIndicator = Char.MinValue;
        if (m.Groups[4] != null && m.Groups[4].Value.Length > 0)
          shortLongIndicator = m.Groups[4].Value[0];
        #endregion

        #region format specifier
        // extract format
        formatSpecifier = Char.MinValue;
        if (m.Groups[5] != null && m.Groups[5].Value.Length > 0)
          formatSpecifier = m.Groups[5].Value[0];
        #endregion

        // default precision is 6 digits if none is specified
        if (fieldPrecision == int.MinValue && formatSpecifier != 's' && formatSpecifier != 'c')
          fieldPrecision = 6;

        #region get next value parameter
        // get next value parameter and convert value parameter depending on short / long indicator
        if (parameters == null || i >= parameters.Length)
          o = null;
        else {
          o = parameters[i];

          if (shortLongIndicator == 'h') {
            if (o is int)
              o = (short)((int)o);
            else if (o is long)
              o = (short)((long)o);
            else if (o is uint)
              o = (ushort)((uint)o);
            else if (o is ulong)
              o = (ushort)((ulong)o);
          } else if (shortLongIndicator == 'l') {
            if (o is short)
              o = (long)((short)o);
            else if (o is int)
              o = (long)((int)o);
            else if (o is ushort)
              o = (ulong)((ushort)o);
            else if (o is uint)
              o = (ulong)((uint)o);
          }
        }
        #endregion

        // convert value parameters to a string depending on the formatSpecifier
        w = String.Empty;
        switch (formatSpecifier) {
          #region % - character
          case '%':   // % character
            w = "%";
            break;
          #endregion
          #region d - integer
          case 'd':   // integer
            w = FormatNumber(o, (flagGroupThousands ? "n" : "d"),
              flagAlternate, fieldLength, int.MinValue,
              flagLeft2Right, flagPositiveSign,
              flagPositiveSpace, paddingCharacter);
            i++;
            break;
          #endregion
          #region i - integer
          case 'i':   // integer
            goto case 'd';
          #endregion
          #region o - octal integer
          case 'o':   // octal integer - no leading zero
            w = FormatOct(o, "o",
              flagAlternate, fieldLength, int.MinValue,
              flagLeft2Right, paddingCharacter);
            i++;
            break;
          #endregion
          #region x - hex integer
          case 'x':   // hex integer - no leading zero
            w = FormatHex(o, "x",
              flagAlternate, fieldLength, int.MinValue,
              flagLeft2Right, paddingCharacter);
            i++;
            break;
          #endregion
          #region X - hex integer
          case 'X':   // same as x but with capital hex characters
            w = FormatHex(o, "X",
              flagAlternate, fieldLength, int.MinValue,
              flagLeft2Right, paddingCharacter);
            i++;
            break;
          #endregion
          #region u - unsigned integer
          case 'u':   // unsigned integer
            w = FormatNumber(ToUnsigned(o), (flagGroupThousands ? "n" : "d"),
              flagAlternate, fieldLength, int.MinValue,
              flagLeft2Right, false,
              false, paddingCharacter);
            i++;
            break;
          #endregion
          #region c - character
          case 'c':   // character
            if (IsNumericType(o))
              w = Convert.ToChar(o).ToString();
            else if (o is char)
              w = ((char)o).ToString();
            else if (o is string && ((string)o).Length > 0)
              w = ((string)o)[0].ToString();
            i++;
            break;
          #endregion
          #region s - string
          case 's':   // string
            string t = "{0" + (fieldLength != int.MinValue ? "," + (flagLeft2Right ? "-" : String.Empty) + fieldLength.ToString() : String.Empty) + ":s}";
            w = o.ToString();
            if (fieldPrecision >= 0)
              w = w.Substring(0, fieldPrecision);

            if (fieldLength != int.MinValue)
              if (flagLeft2Right)
                w = w.PadRight(fieldLength, paddingCharacter);
              else
                w = w.PadLeft(fieldLength, paddingCharacter);
            i++;
            break;
          #endregion
          #region f - double number
          case 'f':   // double
            w = FormatNumber(o, (flagGroupThousands ? "n" : "f"),
              flagAlternate, fieldLength, fieldPrecision,
              flagLeft2Right, flagPositiveSign,
              flagPositiveSpace, paddingCharacter);
            i++;
            break;
          #endregion
          #region e - exponent number
          case 'e':   // double / exponent
            w = FormatNumber(o, "e",
              flagAlternate, fieldLength, fieldPrecision,
              flagLeft2Right, flagPositiveSign,
              flagPositiveSpace, paddingCharacter);
            i++;
            break;
          #endregion
          #region E - exponent number
          case 'E':   // double / exponent
            w = FormatNumber(o, "E",
              flagAlternate, fieldLength, fieldPrecision,
              flagLeft2Right, flagPositiveSign,
              flagPositiveSpace, paddingCharacter);
            i++;
            break;
          #endregion
          #region g - general number
          case 'g':   // double / exponent
            w = FormatNumber(o, "g",
              flagAlternate, fieldLength, fieldPrecision,
              flagLeft2Right, flagPositiveSign,
              flagPositiveSpace, paddingCharacter);
            i++;
            break;
          #endregion
          #region G - general number
          case 'G':   // double / exponent
            w = FormatNumber(o, "G",
              flagAlternate, fieldLength, fieldPrecision,
              flagLeft2Right, flagPositiveSign,
              flagPositiveSpace, paddingCharacter);
            i++;
            break;
          #endregion
          #region p - pointer
          case 'p':   // pointer
            if (o is IntPtr)
              w = "0x" + ((IntPtr)o).ToString("x");
            i++;
            break;
          #endregion
          #region n - number of processed chars so far
          case 'n':   // number of characters so far
            w = FormatNumber(m.Index, "d",
              flagAlternate, fieldLength, int.MinValue,
              flagLeft2Right, flagPositiveSign,
              flagPositiveSpace, paddingCharacter);
            break;
          #endregion
          default:
            w = String.Empty;
            i++;
            break;
        }

        // replace format parameter with parameter value
        // and start searching for the next format parameter
        // AFTER the position of the current inserted value
        // to prohibit recursive matches if the value also
        // includes a format specifier
        f.Remove(m.Index, m.Length);
        f.Insert(m.Index, w);
        m = r.Match(f.ToString(), m.Index + w.Length);
      }

      return f.ToString();
    }

    private static bool IsNumericType(object o) {
      return (o is byte ||
          o is sbyte ||
          o is short ||
          o is ushort ||
          o is int ||
          o is uint ||
          o is long ||
          o is ulong ||
          o is float ||
          o is double ||
          o is decimal);
    }

    private static bool IsPositive(object value, bool zeroIsPositive) {
      switch (Type.GetTypeCode(value.GetType())) {
        case TypeCode.SByte:
          return (zeroIsPositive ? (sbyte)value >= 0 : (sbyte)value > 0);
        case TypeCode.Int16:
          return (zeroIsPositive ? (short)value >= 0 : (short)value > 0);
        case TypeCode.Int32:
          return (zeroIsPositive ? (int)value >= 0 : (int)value > 0);
        case TypeCode.Int64:
          return (zeroIsPositive ? (long)value >= 0 : (long)value > 0);
        case TypeCode.Single:
          return (zeroIsPositive ? (float)value >= 0 : (float)value > 0);
        case TypeCode.Double:
          return (zeroIsPositive ? (double)value >= 0 : (double)value > 0);
        case TypeCode.Decimal:
          return (zeroIsPositive ? (decimal)value >= 0 : (decimal)value > 0);
        case TypeCode.Byte:
          return (zeroIsPositive ? true : (byte)value > 0);
        case TypeCode.UInt16:
          return (zeroIsPositive ? true : (ushort)value > 0);
        case TypeCode.UInt32:
          return (zeroIsPositive ? true : (uint)value > 0);
        case TypeCode.UInt64:
          return (zeroIsPositive ? true : (ulong)value > 0);
        case TypeCode.Char:
          return (zeroIsPositive ? true : (char)value != '\0');
        default:
          return false;
      }
    }

    private static object ToUnsigned(object value) {
      switch (Type.GetTypeCode(value.GetType())) {
        case TypeCode.SByte:
          return (byte)((sbyte)value);
        case TypeCode.Int16:
          return (ushort)((short)value);
        case TypeCode.Int32:
          return (uint)((int)value);
        case TypeCode.Int64:
          return (ulong)((long)value);
        case TypeCode.Byte:
          return value;
        case TypeCode.UInt16:
          return value;
        case TypeCode.UInt32:
          return value;
        case TypeCode.UInt64:
          return value;
        case TypeCode.Single:
          return (UInt32)((float)value);
        case TypeCode.Double:
          return (ulong)((double)value);
        case TypeCode.Decimal:
          return (ulong)((decimal)value);
        default:
          return null;
      }
    }

    private static object ToInteger(object value, bool round) {
      switch (Type.GetTypeCode(value.GetType())) {
        case TypeCode.SByte:
          return value;
        case TypeCode.Int16:
          return value;
        case TypeCode.Int32:
          return value;
        case TypeCode.Int64:
          return value;
        case TypeCode.Byte:
          return value;
        case TypeCode.UInt16:
          return value;
        case TypeCode.UInt32:
          return value;
        case TypeCode.UInt64:
          return value;
        case TypeCode.Single:
          return (round ? (int)Math.Round((float)value) : (int)((float)value));
        case TypeCode.Double:
          return (round ? (long)Math.Round((double)value) : (long)((double)value));
        case TypeCode.Decimal:
          return (round ? Math.Round((decimal)value) : (decimal)value);
        default:
          return null;
      }
    }

    private static long ToInt64(object value, bool round) {
      switch (Type.GetTypeCode(value.GetType())) {
        case TypeCode.SByte:
          return (long)((sbyte)value);
        case TypeCode.Int16:
          return (long)((short)value);
        case TypeCode.Int32:
          return (long)((int)value);
        case TypeCode.Int64:
          return (long)value;
        case TypeCode.Byte:
          return (long)((byte)value);
        case TypeCode.UInt16:
          return (long)((ushort)value);
        case TypeCode.UInt32:
          return (long)((uint)value);
        case TypeCode.UInt64:
          return (long)((ulong)value);
        case TypeCode.Single:
          return (round ? (long)Math.Round((float)value) : (long)((float)value));
        case TypeCode.Double:
          return (round ? (long)Math.Round((double)value) : (long)((double)value));
        case TypeCode.Decimal:
          return (round ? (long)Math.Round((decimal)value) : (long)((decimal)value));
        default:
          return 0;
      }
    }

    private static string FormatOct(object value, string nativeFormat, bool alternate, int length, int precision, bool ltr, char padding) {
      string w = String.Empty;
      string lengthFormat = "{0" + (length != int.MinValue ?
                     "," + (ltr ?
                         "-" :
                          String.Empty) + length.ToString() :
                      String.Empty) + "}";

      if (IsNumericType(value)) {
        w = Convert.ToString(ToInt64(value, true), 8);

        if (ltr || padding == ' ') {
          if (alternate && w != "0")
            w = "0" + w;
          w = String.Format(lengthFormat, w);
        } else {
          if (length != int.MinValue)
            w = w.PadLeft(length - (alternate && w != "0" ? 1 : 0), padding);
          if (alternate && w != "0")
            w = "0" + w;
        }
      }

      return w;
    }

    private static string FormatHex(object value, string nativeFormat, bool alternate, int length, int precision, bool ltr, char padding) {
      string w = String.Empty;
      string lengthFormat = "{0" + (length != int.MinValue ?
                                      "," + (ltr ?
                                              "-" :
                                              String.Empty) + length.ToString() :
                                      String.Empty) + "}";
      string numberFormat = "{0:" + nativeFormat + (precision != int.MinValue ?
                                      precision.ToString() :
                                      String.Empty) + "}";

      if (IsNumericType(value)) {
        w = String.Format(numberFormat, value);

        if (ltr || padding == ' ') {
          if (alternate)
            w = (nativeFormat == "x" ? "0x" : "0X") + w;
          w = String.Format(lengthFormat, w);
        } else {
          if (length != int.MinValue)
            w = w.PadLeft(length - (alternate ? 2 : 0), padding);
          if (alternate)
            w = (nativeFormat == "x" ? "0x" : "0X") + w;
        }
      }

      return w;
    }

    private static string FormatNumber(object value, string nativeFormat, bool alternate, int length, int precision, bool ltr, bool positiveSign, bool positiveSpace, char padding) {
      string w = String.Empty;
      string lengthFormat = "{0" + (length != int.MinValue ?
                     "," + (ltr ?
                         "-" :
                          String.Empty) + length.ToString() :
                      String.Empty) + "}";
      string numberFormat = "{0:" + nativeFormat + (precision != int.MinValue ?
                     precision.ToString() :
                      "0") + "}";

      if (IsNumericType(value)) {
        if (nativeFormat == "d") {
          value = ToInteger(value, false);
        }
        w = String.Format(numberFormat, value);

        if (ltr || padding == ' ') {
          if (IsPositive(value, true))
            w = (positiveSign ?
                                "+" : (positiveSpace ? " " : String.Empty)) + w;
          w = String.Format(lengthFormat, w);
        } else {
          if (w.StartsWith("-"))
            w = w.Substring(1);
          if (length != int.MinValue)
            w = w.PadLeft(length - 1, padding);
          if (IsPositive(value, true))
            w = (positiveSign ?
                "+" : (positiveSpace ?
                    " " : (length != int.MinValue ?
                        padding.ToString() : String.Empty))) + w;
          else
            w = "-" + w;
        }
      }

      return w;
    }
  }
}
