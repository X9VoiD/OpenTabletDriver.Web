using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace OpenTabletDriver.Web.Benchmarks;

public class PrecedeTrimBenchmarks
{
    public char LeadingCharacter { get; set; }
    public string Input { get; set; }

    public PrecedeTrimBenchmarks()
    {
        // Prevent JIT from optimizing input away
        Input = new TestInput().Input;
        LeadingCharacter = ' ';
    }

    [Benchmark]
    public string TrimPrecedeVoiD()
    {
        var lines = Input.Split(Environment.NewLine);
        var preceding = CountPrecedingVoiD(lines, LeadingCharacter);
        for (var i = 0; i < lines.Length; ++i)
        {
            var line = lines[i];
            lines[i] = line.Length >= preceding ? line[preceding..] : string.Empty;
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static int CountPrecedingVoiD(string[] lines, char leadingCharacter)
    {
        var min = int.MaxValue;
        foreach (var line in lines)
        {
            var lineSpan = line.AsSpan();
            for (var i = 0; i < lineSpan.Length; ++i)
            {
                if (lineSpan[i] != leadingCharacter)
                {
                    min = i < min ? i : min;
                    break;
                }
            }
        }

        return min;
    }

    [Benchmark]
    public string TrimPrecedeVed()
    {
        var endsTrimmedContent = TrimStartRegex.Replace(Input.TrimEnd(), "$1", 1);
        var lines = endsTrimmedContent.Split(Environment.NewLine);
        var baseIndentationLength = CountIndentationVed(lines[0]);

        for (int i = 0; i != lines.Length; i++)
        {
            var line = lines[i];

            var indentationLength = CountIndentationVed(line);
            if (indentationLength < baseIndentationLength)
            {
                if (indentationLength == line.Length)
                {
                    lines[i] = "";
                    continue;
                }

                return endsTrimmedContent;
            }

            lines[i] = line[Math.Min(baseIndentationLength, line.Length)..].TrimEnd();
        }

        return String.Join(Environment.NewLine, lines);
    }

    private static Regex TrimStartRegex = new("^\\s*\n(\\s*)(?=\\S*)", RegexOptions.Compiled);

    private int CountIndentationVed(string line)
    {
        for (var i = 0; i < line.Length; i++)
            if (!char.IsWhiteSpace(line[i]))
                return i;
        return int.MaxValue;
    }

    [Benchmark]
    public string TrimPrecedeVoiDVectorized()
    {
        var lines = Input.Split(Environment.NewLine);
        var preceding = CountPrecedingVoiDVectorized(lines, LeadingCharacter);
        for (var i = 0; i < lines.Length; ++i)
        {
            var line = lines[i];
            lines[i] = line.Length >= preceding ? line[preceding..] : string.Empty;
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static int CountPrecedingVoiDVectorized(string[] lines, char leadingCharacter)
    {
        var min = int.MaxValue;
        foreach (var line in lines)
        {
            var count = CountPrecedingVectorized(line, leadingCharacter);
            if (count != line.Length && count < min)
            {
                min = count;
            }
        }

        return min;
    }

    private static int CountPrecedingVectorized(string line, char leadingCharacter)
    {
        ReadOnlySpan<char> span = line.AsSpan();
        ref var r0 = ref MemoryMarshal.GetReference(span);
        var length = span.Length;
        int i = 0;

        if (Vector.IsHardwareAccelerated)
        {
            var batchCount = Vector<ushort>.Count;
            var end = length - batchCount;
            var vc = new Vector<ushort>(leadingCharacter);

            for (; i <= end; i += batchCount)
            {
                ref var ri = ref Unsafe.Add(ref r0, i);
                var vi = Unsafe.As<char, Vector<ushort>>(ref ri);

                var ve = Vector.Equals(vi, vc);

                if (ve == Vector<ushort>.Zero)
                {
                    return i;
                }

                if (Vector.LessThanAny(ve, Vector<ushort>.One))
                {
                    break;
                }
            }
        }

        for (; i < length; ++i)
        {
            if (span[i] != leadingCharacter)
            {
                break;
            }
        }

        return i;
    }

    [Benchmark]
    public unsafe string TrimPrecedeVoiDSpan()
    {
        var buffer = stackalloc char[Input.Length];
        var enumerator = Input.AsSpan().EnumerateLines();
        var enumeratorCopy = enumerator;
        var preceding = 0;
        foreach (var line in enumerator)
        {
            var tmpPrecede = CountPrecedingVoiDSpan(line, LeadingCharacter);
            if (tmpPrecede != 0)
            {
                preceding = tmpPrecede;
                break;
            }
        }

        char* bufferPtr = buffer;
        {
            char* head = bufferPtr;
            foreach (var line in enumeratorCopy)
            {
                if (line.Length < preceding)
                {
                    *head = '\n';
                    head += 1;
                    continue;
                }
                var newLength = line.Length - preceding;
                var headPtr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(line));
                VectorCopy(headPtr + preceding, head, newLength * 2);
                head += newLength;
                *head = '\n';
                head += 1;
            }

            *head = '\0';
        }

        return new string(buffer);
    }

    private static int CountPrecedingVoiDSpan(ReadOnlySpan<char> line, char leadingCharacter)
    {
        for (var i = 0; i < line.Length; ++i)
        {
            if (line[i] != leadingCharacter)
            {
                return i;
            }
        }

        return -1;
    }

    private unsafe static void VectorCopy(void* source, void* destination, int length)
    {
        var i = 0;
        byte* s = (byte*)source;
        byte* d = (byte*)destination;
        if (Vector.IsHardwareAccelerated)
        {
            var batchCount = Vector<byte>.Count;
            var end = length - batchCount;

            for (; i <= end; i += batchCount)
            {
                var vs = (Vector<byte>*)(s + i);
                vs->CopyTo(new Span<byte>(d + i, batchCount));
            }
        }

        for (; i < length; ++i)
        {
            d[i] = s[i];
        }
    }
}
