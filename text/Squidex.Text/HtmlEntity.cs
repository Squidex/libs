// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Squidex.Text;

public static class HtmlEntity
{
    private static readonly Dictionary<ReadOnlyMemory<char>, int> HtmlEntityNames = new Dictionary<ReadOnlyMemory<char>, int>(ReadOnlyMemoryComparer.Instance);
    private static readonly int MaxEntityLength;

    private class ReadOnlyMemoryComparer : IEqualityComparer<ReadOnlyMemory<char>>
    {
        public static readonly ReadOnlyMemoryComparer Instance = new ReadOnlyMemoryComparer();

        public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
        {
            return x.Span.Equals(y.Span, StringComparison.Ordinal);
        }

        public int GetHashCode([DisallowNull] ReadOnlyMemory<char> obj)
        {
            return string.GetHashCode(obj.Span, StringComparison.Ordinal);
        }
    }

    static HtmlEntity()
    {
        static void Add(string entity, int code)
        {
            HtmlEntityNames.Add(entity.AsMemory(), code);
        }

        Add("Aacute", 193);
        Add("aacute", 225);
        Add("Acirc", 194);
        Add("acirc", 226);
        Add("acute", 180);
        Add("AElig", 198);
        Add("aelig", 230);
        Add("Agrave", 192);
        Add("agrave", 224);
        Add("alefsym", 8501);
        Add("Alpha", 913);
        Add("alpha", 945);
        Add("amp", 38);
        Add("and", 8743);
        Add("ang", 8736);
        Add("apos", 39);
        Add("Aring", 197);
        Add("aring", 229);
        Add("asymp", 8776);
        Add("Atilde", 195);
        Add("atilde", 227);
        Add("Auml", 196);
        Add("auml", 228);
        Add("bdquo", 8222);
        Add("Beta", 914);
        Add("beta", 946);
        Add("brvbar", 166);
        Add("bull", 8226);
        Add("cap", 8745);
        Add("Ccedil", 199);
        Add("ccedil", 231);
        Add("cedil", 184);
        Add("cent", 162);
        Add("Chi", 935);
        Add("chi", 967);
        Add("circ", 710);
        Add("clubs", 9827);
        Add("cong", 8773);
        Add("copy", 169);
        Add("crarr", 8629);
        Add("cup", 8746);
        Add("curren", 164);
        Add("dagger", 8224);
        Add("Dagger", 8225);
        Add("darr", 8595);
        Add("dArr", 8659);
        Add("deg", 176);
        Add("Delta", 916);
        Add("delta", 948);
        Add("diams", 9830);
        Add("divide", 247);
        Add("Eacute", 201);
        Add("eacute", 233);
        Add("Ecirc", 202);
        Add("ecirc", 234);
        Add("Egrave", 200);
        Add("egrave", 232);
        Add("empty", 8709);
        Add("emsp", 8195);
        Add("ensp", 8194);
        Add("Epsilon", 917);
        Add("epsilon", 949);
        Add("equiv", 8801);
        Add("Eta", 919);
        Add("eta", 951);
        Add("ETH", 208);
        Add("eth", 240);
        Add("Euml", 203);
        Add("euml", 235);
        Add("euro", 8364);
        Add("exist", 8707);
        Add("fnof", 402);
        Add("forall", 8704);
        Add("frac12", 189);
        Add("frac14", 188);
        Add("frac34", 190);
        Add("frasl", 8260);
        Add("Gamma", 915);
        Add("gamma", 947);
        Add("ge", 8805);
        Add("gt", 62);
        Add("harr", 8596);
        Add("hArr", 8660);
        Add("hearts", 9829);
        Add("hellip", 8230);
        Add("Iacute", 205);
        Add("iacute", 237);
        Add("Icirc", 206);
        Add("icirc", 238);
        Add("iexcl", 161);
        Add("Igrave", 204);
        Add("igrave", 236);
        Add("image", 8465);
        Add("infin", 8734);
        Add("int", 8747);
        Add("Iota", 921);
        Add("iota", 953);
        Add("iquest", 191);
        Add("isin", 8712);
        Add("Iuml", 207);
        Add("iuml", 239);
        Add("Kappa", 922);
        Add("kappa", 954);
        Add("Lambda", 923);
        Add("lambda", 955);
        Add("lang", 9001);
        Add("laquo", 171);
        Add("larr", 8592);
        Add("lArr", 8656);
        Add("lceil", 8968);
        Add("ldquo", 8220);
        Add("le", 8804);
        Add("lfloor", 8970);
        Add("lowast", 8727);
        Add("loz", 9674);
        Add("lrm", 8206);
        Add("lsaquo", 8249);
        Add("lsquo", 8216);
        Add("lt", 60);
        Add("macr", 175);
        Add("mdash", 8212);
        Add("micro", 181);
        Add("middot", 183);
        Add("minus", 8722);
        Add("Mu", 924);
        Add("mu", 956);
        Add("nabla", 8711);
        Add("nbsp", 160);
        Add("ndash", 8211);
        Add("ne", 8800);
        Add("ni", 8715);
        Add("not", 172);
        Add("notin", 8713);
        Add("nsub", 8836);
        Add("Ntilde", 209);
        Add("ntilde", 241);
        Add("Nu", 925);
        Add("nu", 957);
        Add("Oacute", 211);
        Add("oacute", 243);
        Add("Ocirc", 212);
        Add("ocirc", 244);
        Add("OElig", 338);
        Add("oelig", 339);
        Add("Ograve", 210);
        Add("ograve", 242);
        Add("oline", 8254);
        Add("Omega", 937);
        Add("omega", 969);
        Add("Omicron", 927);
        Add("omicron", 959);
        Add("oplus", 8853);
        Add("or", 8744);
        Add("ordf", 170);
        Add("ordm", 186);
        Add("Oslash", 216);
        Add("oslash", 248);
        Add("Otilde", 213);
        Add("otilde", 245);
        Add("otimes", 8855);
        Add("Ouml", 214);
        Add("ouml", 246);
        Add("para", 182);
        Add("part", 8706);
        Add("permil", 8240);
        Add("perp", 8869);
        Add("Phi", 934);
        Add("phi", 966);
        Add("Pi", 928);
        Add("pi", 960);
        Add("piv", 982);
        Add("plusmn", 177);
        Add("pound", 163);
        Add("prime", 8242);
        Add("Prime", 8243);
        Add("prod", 8719);
        Add("prop", 8733);
        Add("Psi", 936);
        Add("psi", 968);
        Add("quot", 34);
        Add("radic", 8730);
        Add("rang", 9002);
        Add("raquo", 187);
        Add("rarr", 8594);
        Add("rArr", 8658);
        Add("rceil", 8969);
        Add("rdquo", 8221);
        Add("real", 8476);
        Add("reg", 174);
        Add("rfloor", 8971);
        Add("Rho", 929);
        Add("rho", 961);
        Add("rlm", 8207);
        Add("rsaquo", 8250);
        Add("rsquo", 8217);
        Add("sbquo", 8218);
        Add("Scaron", 352);
        Add("scaron", 353);
        Add("sdot", 8901);
        Add("sect", 167);
        Add("shy", 173);
        Add("Sigma", 931);
        Add("sigma", 963);
        Add("sigmaf", 962);
        Add("sim", 8764);
        Add("spades", 9824);
        Add("sub", 8834);
        Add("sube", 8838);
        Add("sum", 8721);
        Add("sup", 8835);
        Add("sup1", 185);
        Add("sup2", 178);
        Add("sup3", 179);
        Add("supe", 8839);
        Add("szlig", 223);
        Add("Tau", 932);
        Add("tau", 964);
        Add("there4", 8756);
        Add("Theta", 920);
        Add("theta", 952);
        Add("thetasym", 977);
        Add("thinsp", 8201);
        Add("THORN", 222);
        Add("thorn", 254);
        Add("tilde", 732);
        Add("times", 215);
        Add("trade", 8482);
        Add("Uacute", 218);
        Add("uacute", 250);
        Add("uarr", 8593);
        Add("uArr", 8657);
        Add("Ucirc", 219);
        Add("ucirc", 251);
        Add("Ugrave", 217);
        Add("ugrave", 249);
        Add("uml", 168);
        Add("upsih", 978);
        Add("Upsilon", 933);
        Add("upsilon", 965);
        Add("Uuml", 220);
        Add("uuml", 252);
        Add("weierp", 8472);
        Add("Xi", 926);
        Add("xi", 958);
        Add("Yacute", 221);
        Add("yacute", 253);
        Add("yen", 165);
        Add("yuml", 255);
        Add("Yuml", 376);
        Add("Zeta", 918);
        Add("zeta", 950);
        Add("zwj", 8205);
        Add("zwnj", 8204);

        MaxEntityLength = HtmlEntityNames.Keys.Max(x => x.Length);
    }

    private enum ParserState
    {
        Text,
        EntityStart
    }

    public static void Decode(string source, StringBuilder target)
    {
        var memory = source.AsMemory();

        target.EnsureCapacity(target.Length + source.Length);

        var entityStart = -1;

        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];

            if (entityStart < 0)
            {
                if (c == '&')
                {
                    entityStart = i;
                }
                else
                {
                    target.Append(c);
                }
            }
            else if (c == ';')
            {
                var entity = memory[entityStart.. (i + 1)];

                if (entity.Length > MaxEntityLength || entity.Length < 2)
                {
                    target.Append(entity);
                    continue;
                }

                var asSpan = entity.Span;

                var isNumber = asSpan[1] == '#';

                if (isNumber)
                {
                    asSpan = asSpan[2..^1];

                    var styles = NumberStyles.Integer;

                    if (asSpan.Length > 0 && asSpan[0] == 'x')
                    {
                        styles = NumberStyles.HexNumber;

                        asSpan = asSpan[1..];
                    }

                    if (int.TryParse(asSpan, styles, CultureInfo.InvariantCulture, out var code))
                    {
                        target.Append(Convert.ToChar(code));
                    }
                    else
                    {
                        target.Append(entity);
                    }
                }
                else
                {
                    var name = entity[1..^1];

                    if (HtmlEntityNames.TryGetValue(name, out var code))
                    {
                        target.Append(Convert.ToChar(code));
                    }
                    else
                    {
                        target.Append(entity);
                    }
                }

                entityStart = -1;
            }
        }
    }
}
