using lzyCodec;

namespace sampleApp;

class Program
{
    static void Main(string[] args)
    {
        var inputRunes = new List<int> { 0x41, 0x42, 0x43 }; // 'A','B','C'
        var result = Lzy.Encode(inputRunes);
        Console.WriteLine($"result {result}");

        var resultDecode = Lzy.Decode(result);
        Console.WriteLine($"resultDecode {resultDecode}");
    }
}