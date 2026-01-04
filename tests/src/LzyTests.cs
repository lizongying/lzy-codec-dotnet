using System.Text;

namespace tests;

using lzyCodec;
using Xunit;

public class LzyTests
{
    #region 1. ValidUnicode 方法测试（Unicode 合法性校验）

    [Fact]
    public void ValidUnicode_ValidCodePoint_ReturnsTrue()
    {
        // 全部使用 var 简化变量声明
        var validCodePoints = new[] { 0x41, 0x4E2D, 0x1F600, 0, 0x10FFFF };
        foreach (var code in validCodePoints)
        {
            Assert.True(Lzy.ValidUnicode(code));
        }
    }

    [Fact]
    public void ValidUnicode_InvalidSurrogate_ReturnsFalse()
    {
        var invalidSurrogates = new[] { 0xD800, 0xDFFF, 0xDBFF };
        foreach (var code in invalidSurrogates)
        {
            Assert.False(Lzy.ValidUnicode(code));
        }
    }

    [Fact]
    public void ValidUnicode_OutOfRange_ReturnsFalse()
    {
        var outOfRangeCodes = new[] { -1, 0x110000 };
        foreach (var code in outOfRangeCodes)
        {
            Assert.False(Lzy.ValidUnicode(code));
        }
    }

    #endregion

    #region 2. Encode 系列方法测试（编码功能）

    [Fact]
    public void Encode_AsciiInput_ReturnsCorrectBytes()
    {
        var inputRunes = new List<int> { 0x41, 0x42, 0x43 }; // 'A','B','C'
        var result = Lzy.Encode(inputRunes);
        var expected = new byte[] { 0x41, 0x42, 0x43 };
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EncodeFromString_ChineseInput_ReturnsCorrectBytes()
    {
        var inputStr = "中国";
        var result = Lzy.EncodeFromString(inputStr);

        // 闭环测试：编码后解码与原字符串一致
        var decodeStr = Lzy.DecodeToString(result);
        Assert.Equal(inputStr, decodeStr);
    }

    [Fact]
    public void EncodeFromString_EmojiInput_ReturnsCorrectBytes()
    {
        var inputStr = "😀测试";
        var result = Lzy.EncodeFromString(inputStr);

        var decodeStr = Lzy.DecodeToString(result);
        Assert.Equal(inputStr, decodeStr);
    }

    [Fact]
    public void EncodeFromBytes_Utf8Bytes_ReturnsCorrectEncodedBytes()
    {
        var originalStr = "Hello 世界！";
        var utf8Bytes = Encoding.UTF8.GetBytes(originalStr);

        var encodedBytes = Lzy.EncodeFromBytes(utf8Bytes);
        var decodedStr = Lzy.DecodeToString(encodedBytes);

        Assert.Equal(originalStr, decodedStr);
    }

    #endregion

    #region 3. Decode 系列方法测试（解码功能）

    [Fact]
    public void Decode_ValidEncodedBytes_ReturnsCorrectRunes()
    {
        var inputStr = "ABC中文";
        var encodedBytes = Lzy.EncodeFromString(inputStr);
        var runes = Lzy.Decode(encodedBytes);

        // 验证 Rune 列表可还原为原字符串
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
                var high = (char)(0xD800 + (offset >> 10));
                var low = (char)(0xDC00 + (offset & 0x3FF));
                sb.Append(high).Append(low);
            }
        }

        Assert.Equal(inputStr, sb.ToString());
    }

    [Fact]
    public void DecodeToString_ValidEncodedBytes_ReturnsCorrectString()
    {
        // 多测试用例批量验证
        var testCases = new[] { "ASCII", "中文测试", "😀Emoji", "混合测试123！" };
        foreach (var testStr in testCases)
        {
            var encoded = Lzy.EncodeFromString(testStr);
            var decoded = Lzy.DecodeToString(encoded);
            Assert.Equal(testStr, decoded);
        }
    }

    [Fact]
    public void DecodeToBytes_ValidEncodedBytes_ReturnsCorrectUtf8Bytes()
    {
        var originalStr = "解码测试：123 中文 😀";
        var encodedBytes = Lzy.EncodeFromString(originalStr);
        var decodedUtf8Bytes = Lzy.DecodeToBytes(encodedBytes);

        var recoveredStr = Encoding.UTF8.GetString(decodedUtf8Bytes);
        Assert.Equal(originalStr, recoveredStr);
    }

    #endregion

    #region 4. 异常场景测试

    [Fact]
    public void Decode_EmptyBytes_ThrowsArgumentException()
    {
        var emptyBytes = Array.Empty<byte>();
        Assert.Throws<ArgumentException>(() => Lzy.Decode(emptyBytes));
    }

    [Fact]
    public void Decode_InvalidBytes_ThrowsArgumentException()
    {
        var invalidBytes = new byte[] { 0x80, 0x81, 0x82 }; // 无效高位字节
        Assert.Throws<ArgumentException>(() => Lzy.Decode(invalidBytes));
    }

    [Fact]
    public void Decode_InvalidUnicodeRune_ThrowsArgumentException()
    {
        var invalidRunes = new List<int> { 0xD800 }; // 代理区无效字符
        var encodedBytes = Lzy.Encode(invalidRunes);

        Assert.Throws<ArgumentException>(() => Lzy.Decode(encodedBytes));
    }

    #endregion
}