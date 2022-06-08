using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace OpenTabletDriver.Web.TagHelpers
{
    [HtmlTargetElement("codeblock")]
    public class CodeBlockTagHelper : TagHelper
    {
        public string Language { set; get; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "pre";

            var content = await output.GetChildContentAsync();
            var innerHtml = content.GetContent().Trim('\n');
            var body = TrimPreceding(innerHtml, ' ');

            var code = new TagBuilder("code");
            code.AddCssClass("hljs");
            code.AddCssClass($"language-{Language}");
            code.InnerHtml.SetHtmlContent(body);

            output.Content.SetHtmlContent(code);
        }

        private static string TrimPreceding(string value, char character)
        {
            var lines = value.Split(Environment.NewLine);
            var preceding = CountPreceding(lines, character);
            for (var i = 0; i < lines.Length; ++i)
            {
                var line = lines[i];
                lines[i] = line.Length >= preceding ? line[preceding..] : string.Empty;
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static int CountPreceding(string[] lines, char leadingCharacter)
        {
            var min = int.MaxValue;
            foreach (var line in lines)
            {
                var count = CountPreceding(line, leadingCharacter);
                if (count != line.Length && count < min)
                {
                    min = count;
                }
            }

            return min;
        }

        private static int CountPreceding(string line, char leadingCharacter)
        {
            ReadOnlySpan<char> span = line.AsSpan();
            ref var r0 = ref MemoryMarshal.GetReference(span);
            var length = span.Length;
            int i = 0;

            if (Vector.IsHardwareAccelerated)
            {
                var end = length - Vector<ushort>.Count;
                var match = Vector<ushort>.One;
                var vc = new Vector<ushort>(leadingCharacter);

                for (; i <= end; i += Vector<ushort>.Count)
                {
                    ref var ri = ref Unsafe.Add(ref r0, i);
                    var vi = Unsafe.As<char, Vector<ushort>>(ref ri);

                    var ve = Vector.Equals(vi, vc);

                    if (Vector.LessThanAny(ve, match))
                    {
                        i -= Vector<ushort>.Count;
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
    }
}
