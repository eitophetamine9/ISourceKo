using System.Text;
using System.Text.RegularExpressions;
using ISourceKO.Models;

namespace ISourceKO.Services.Security;

public record PromptGuardResult(bool IsAllowed, string SanitizedText, string? BlockReason = null);

/// <summary>
/// Multi-layer prompt-injection defense for both user questions (direct injection)
/// and retrieved external academic abstracts (indirect injection).
/// </summary>
public static partial class PromptSecurityGuard
{
    public const int MaxQuestionLength = 1000;
    private const int MaxSourceTitleLength = 220;
    private const int MaxSourceAbstractLength = 700;

    private static readonly Regex[] InjectionPatterns =
    {
        new(@"ignore\s+(all\s+|the\s+)?(previous|prior|above)\s+(instructions?|prompts?|rules?)", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
        new(@"disregard\s+(all\s+|the\s+)?(previous|prior|above)\s+(instructions?|prompts?|rules?)", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
        new(@"override\s+(system|developer)\s+(instructions?|prompts?|rules?)", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
        new(@"(reveal|print|show|output|leak)\s+(your\s+)?(system\s+prompt|hidden\s+instructions?|api\s+key|environment\s+variables?)", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
        new(@"you\s+are\s+now\s+(in\s+)?(developer\s+mode|unrestricted|jailbroken|dan\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
        new(@"<\s*/?\s*(system|developer|assistant|untrusted_source|user_question)\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)),
        new(@"(\[/?INST\]|<<SYS>>|<\|im_start\|>|<\|system\|>|###\s*SYSTEM\s*:)", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100))
    };

    private static readonly Regex HtmlTagRegex = new(@"<[^>]+>", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private static readonly Regex ScriptBlockRegex = new(@"<\s*(script|style|iframe)[^>]*>.*?<\s*/\s*\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private static readonly Regex DangerousUriRegex = new(@"(javascript|vbscript|data\s*:\s*text/html)\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private static readonly Regex CitationRefRegex = new(@"\[(\d{1,3})\]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Layer 1: Validates and sanitizes the user's question before it enters the LLM prompt.
    /// </summary>
    public static PromptGuardResult ValidateUserQuestion(string? rawQuestion)
    {
        if (string.IsNullOrWhiteSpace(rawQuestion))
        {
            return new PromptGuardResult(false, string.Empty, "Please enter a question about the retrieved sources.");
        }

        var cleaned = StripControlAndZeroWidth(rawQuestion).Trim();
        if (cleaned.Length == 0)
        {
            return new PromptGuardResult(false, string.Empty, "Please enter a valid question.");
        }

        if (cleaned.Length > MaxQuestionLength)
        {
            cleaned = cleaned.Substring(0, MaxQuestionLength);
        }

        foreach (var pattern in InjectionPatterns)
        {
            try
            {
                if (pattern.IsMatch(cleaned))
                {
                    return new PromptGuardResult(
                        false,
                        string.Empty,
                        "Your message triggered ISourceKO's prompt-security filter. Please ask a direct academic question about the sources on screen.");
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return new PromptGuardResult(false, string.Empty, "Input validation timed out. Please shorten your question.");
            }
        }

        return new PromptGuardResult(true, EscapeDelimiters(cleaned));
    }

    /// <summary>
    /// Layer 2: Sanitizes external academic source metadata to defend against indirect prompt injection
    /// embedded in third-party abstracts or titles, and isolates each source in XML data tags.
    /// </summary>
    public static string BuildSandboxedSourcesContext(IReadOnlyList<SourceItem> sources, int maxSources = 10)
    {
        var sb = new StringBuilder();
        var count = Math.Min(sources.Count, maxSources);

        for (int i = 0; i < count; i++)
        {
            var s = sources[i];
            var title = EscapeDelimiters(Truncate(StripControlAndZeroWidth(s.Title), MaxSourceTitleLength));
            var authors = EscapeDelimiters(Truncate(StripControlAndZeroWidth(s.AuthorOrPublisher), 140));
            var abs = EscapeDelimiters(Truncate(StripControlAndZeroWidth(s.Description), MaxSourceAbstractLength));
            var year = s.PublishedDate?.Year.ToString() ?? "n.d.";
            var doi = s.Metadata.TryGetValue("DOI", out var d) ? EscapeDelimiters(d) : "";

            sb.AppendLine($"<untrusted_source id=\"{i + 1}\" platform=\"{s.Platform}\" year=\"{year}\">");
            sb.AppendLine($"Title: {title}");
            sb.AppendLine($"Authors: {authors}");
            if (!string.IsNullOrWhiteSpace(doi))
            {
                sb.AppendLine($"DOI: {doi}");
            }
            sb.AppendLine($"Abstract: {abs}");
            sb.AppendLine("</untrusted_source>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Layer 4: Strips HTML/script payloads and validates citation indices [N] in model output.
    /// </summary>
    public static (string SafeText, List<int> ValidCitations) SanitizeModelOutput(string? rawOutput, int sourceCount)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
        {
            return ("No response was returned by the model.", new List<int>());
        }

        var text = StripControlAndZeroWidth(rawOutput);
        text = ScriptBlockRegex.Replace(text, string.Empty);
        text = HtmlTagRegex.Replace(text, string.Empty);
        text = DangerousUriRegex.Replace(text, "[blocked-uri]:");

        var validCitations = new List<int>();
        text = CitationRefRegex.Replace(text, match =>
        {
            if (int.TryParse(match.Groups[1].Value, out var idx) && idx >= 1 && idx <= sourceCount)
            {
                if (!validCitations.Contains(idx))
                {
                    validCitations.Add(idx);
                }
                return $"[{idx}]";
            }
            return string.Empty;
        });

        validCitations.Sort();
        return (text.Trim(), validCitations);
    }

    private static string StripControlAndZeroWidth(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (ch == '\n' || ch == '\r' || ch == '\t')
            {
                sb.Append(ch);
                continue;
            }
            if (char.IsControl(ch)) continue;
            if (ch >= '\u200B' && ch <= '\u200F') continue;
            if (ch >= '\u202A' && ch <= '\u202E') continue;
            if (ch == '\uFEFF') continue;
            sb.Append(ch);
        }
        return sb.ToString();
    }

    private static string EscapeDelimiters(string text)
        => text.Replace("<", "[").Replace(">", "]");

    private static string Truncate(string text, int maxLen)
        => text.Length <= maxLen ? text : text.Substring(0, maxLen) + "...";
}
