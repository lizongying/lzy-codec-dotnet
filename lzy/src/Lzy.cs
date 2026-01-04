namespace lzyCodec;

using System;
using System.Collections.Generic;
using System.Text;

public class Lzy
{
    private const int SurrogateMin = 0xD800;
    private const int SurrogateMax = 0xDFFF;
    private const int UnicodeMax = 0x10FFFF;
    private static readonly ArgumentException ErrorUnicode = new("invalid unicode");

    public static bool ValidUnicode(int r)
    {
        return (r >= 0 && r < SurrogateMin) || (r > SurrogateMax && r <= UnicodeMax);
    }

    public static byte[] Encode(List<int> inputRunes)
    {
        var byteList = new List<byte>();
        foreach (var r in inputRunes)
        {
            if (r < 0x80)
            {
                byteList.Add((byte)(r & 0xFF));
            }
            else if (r < 0x4000)
            {
                byteList.Add((byte)((r >> 7) & 0xFF));
                byteList.Add((byte)((0x80 | (r & 0x7F)) & 0xFF));
            }
            else
            {
                byteList.Add((byte)((r >> 14) & 0xFF));
                byteList.Add((byte)((0x80 | ((r >> 7) & 0x7F)) & 0xFF));
                byteList.Add((byte)((0x80 | (r & 0x7F)) & 0xFF));
            }
        }

        return byteList.ToArray();
    }

    public static byte[] EncodeFromString(string inputStr)
    {
        var runes = new List<int>();
        for (var i = 0; i < inputStr.Length; i++)
        {
            var code = (int)inputStr[i];
            if (code >= SurrogateMin && code <= SurrogateMax && i + 1 < inputStr.Length)
            {
                var lowCode = (int)inputStr[i + 1];
                var fullRune = ((code - SurrogateMin) << 10) + (lowCode - 0xDC00) + 0x10000;
                runes.Add(fullRune);
                i++;
            }
            else
            {
                runes.Add(code);
            }
        }

        return Encode(runes);
    }

    public static byte[] EncodeFromBytes(byte[] inputBytes)
    {
        var str = Encoding.UTF8.GetString(inputBytes);
        return EncodeFromString(str);
    }

    public static List<int> Decode(byte[] inputBytes)
    {
        if (inputBytes.Length == 0)
        {
            throw ErrorUnicode;
        }

        var startIdx = -1;
        for (var i = 0; i < inputBytes.Length; i++)
        {
            if ((inputBytes[i] & 0x80) == 0)
            {
                startIdx = i;
                break;
            }
        }

        if (startIdx == -1)
        {
            throw ErrorUnicode;
        }

        var output = new List<int>();
        var r = 0;
        for (var i = startIdx; i < inputBytes.Length; i++)
        {
            var b = inputBytes[i] & 0xFF;
            if ((b >> 7) == 0)
            {
                if (i > startIdx)
                {
                    if (!ValidUnicode(r))
                    {
                        throw ErrorUnicode;
                    }

                    output.Add(r);
                }

                r = b;
            }
            else
            {
                if (r > (UnicodeMax >> 7))
                {
                    throw ErrorUnicode;
                }

                r = (r << 7) | (b & 0x7F);
            }
        }

        if (!ValidUnicode(r))
        {
            throw ErrorUnicode;
        }

        output.Add(r);
        return output;
    }

    public static string DecodeToString(byte[] inputBytes)
    {
        var runes = Decode(inputBytes);
        var sb = new StringBuilder();
        foreach (var r in runes)
        {
            if (r <= 0xFFFF)
            {
                sb.Append((char)r);
            }
            else
            {
                var offset = r - 0x10000;
                var high = (char)(SurrogateMin + (offset >> 10));
                var low = (char)(0xDC00 + (offset & 0x3FF));
                sb.Append(high).Append(low);
            }
        }

        return sb.ToString();
    }

    public static byte[] DecodeToBytes(byte[] inputBytes)
    {
        var str = DecodeToString(inputBytes);
        return Encoding.UTF8.GetBytes(str);
    }
}