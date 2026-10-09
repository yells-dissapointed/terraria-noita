#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace terrarianoita.Core;

public sealed class NoitaXmlNode
{
    public string Name { get; set; } = "";
    public Dictionary<string, string> Attributes { get; set; } = new(StringComparer.Ordinal);
    public List<NoitaXmlNode> Children { get; set; } = new();
    public string Text { get; set; } = "";
    public string Get(string key, string fallback = "") => Attributes.TryGetValue(key, out var value) ? value : fallback;
    public double Number(string key, double fallback = 0) => double.TryParse(Get(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && double.IsFinite(n) ? n : fallback;
    public bool Flag(string key, bool fallback = false) => Attributes.ContainsKey(key) ? Get(key).Trim() is "1" or "true" : fallback;
    public bool Enabled => !Attributes.ContainsKey("_enabled") || Flag("_enabled");
    public NoitaXmlNode Copy() => new() { Name = Name, Attributes = new(Attributes, StringComparer.Ordinal), Children = Children.Select(n => n.Copy()).ToList(), Text = Text };
}

/// <summary>Bounded data reader for the permissive XML dialect in the supplied game files.</summary>
public static class NoitaXml
{
    private static readonly Regex Comments = new(@"<!--.*?-->", RegexOptions.Singleline, TimeSpan.FromSeconds(1));
    private static readonly Regex Attrs = new("([A-Za-z_][A-Za-z0-9_.:-]*)\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)')", RegexOptions.Singleline, TimeSpan.FromSeconds(1));
    public static NoitaXmlNode Parse(string source, List<string>? warnings = null)
    {
        if (source.Length > 2_000_000) throw new InvalidOperationException("XML exceeds import size limit");
        source = Comments.Replace(source.TrimStart('\uFEFF'), "");
        var stack = new Stack<NoitaXmlNode>(); NoitaXmlNode? root = null; int nodes = 0, position = 0;
        while (position < source.Length)
        {
            int start = source.IndexOf('<', position);
            if (start < 0) break;
            if (stack.Count > 0) stack.Peek().Text += WebUtility.HtmlDecode(source[position..start]).Trim();
            if (source.AsSpan(start).StartsWith("<![CDATA["))
            {
                int end = source.IndexOf("]]>", start + 9, StringComparison.Ordinal);
                if (end < 0 || stack.Count == 0) throw new InvalidOperationException("Invalid XML CDATA");
                stack.Peek().Text += source[(start + 9)..end]; position = end + 3; continue;
            }
            int finish = start + 1; char quote = '\0';
            for (; finish < source.Length; finish++)
            {
                char c = source[finish];
                if (quote != '\0') { if (c == quote) quote = '\0'; }
                else if (c is '\'' or '"') quote = c;
                else if (c == '>') break;
            }
            if (finish == source.Length) throw new InvalidOperationException("Unterminated XML tag");
            string tag = source[(start + 1)..finish].Trim(); position = finish + 1;
            if (tag.StartsWith("?")) continue;
            if (tag.StartsWith("!")) throw new InvalidOperationException("XML declarations/entities are not supported");
            if (tag.StartsWith("/"))
            {
                if (stack.Count == 0 || stack.Pop().Name != tag[1..].Trim()) throw new InvalidOperationException("Unbalanced XML closing tag: " + tag);
                continue;
            }
            bool selfClosing = tag.EndsWith("/"); if (selfClosing) tag = tag[..^1].TrimEnd();
            int split = 0; while (split < tag.Length && !char.IsWhiteSpace(tag[split])) split++;
            var node = new NoitaXmlNode { Name = tag[..split] };
            if (node.Name.Length == 0 || ++nodes > 20000 || stack.Count >= 32) throw new InvalidOperationException("XML structure exceeds import limits");
            foreach (Match match in Attrs.Matches(tag[split..]))
            {
                string key = match.Groups[1].Value;
                if (node.Attributes.ContainsKey(key)) warnings?.Add($"Duplicate XML attribute {node.Name}.{key}: last value used; native precedence unverified");
                node.Attributes[key] = WebUtility.HtmlDecode(match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value);
            }
            if (stack.Count > 0) stack.Peek().Children.Add(node);
            else if (root != null) throw new InvalidOperationException("Multiple XML roots");
            else root = node;
            if (!selfClosing) stack.Push(node);
        }
        if (root == null || stack.Count != 0) throw new InvalidOperationException("Incomplete XML document");
        return root;
    }
}
