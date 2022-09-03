using System.Text.RegularExpressions;

namespace SDParamsDescripter.Core.Models;
public class PromptSplitter
{
    public int PromptLimit
    {
        get; init;
    }
    /// <summary>
    /// 設定文字数限界 - promptインデックス文字数 - 最後のカンマ
    /// </summary>
    private int Limit => PromptLimit - CharCount(PromptLabel) - 1;
    private static string PromptLabel => "prompt(0/0):\r\n";

    public PromptSplitter(int promptLimit = 280)
    {
        PromptLimit = promptLimit;
    }

    public IEnumerable<string> Split(string prompt)
    {
        var prompts = SplitPrompt(Enumerable.Empty<string>(), prompt.Replace(", ", ",").Replace(",", ",\u00A0").Split("\u00A0")).Prompts.ToArray();
        return prompts.Select((p, i) => $"prompt({i + 1}/{prompts.Length}):\r\n{p}");
    }

    private (IEnumerable<string> Prompts, IEnumerable<string> Remainds) SplitPrompt(IEnumerable<string> prompts, IEnumerable<string> remainds)
    {
        if (!remainds.Any())
        {
            return (prompts, remainds);
        }

        var firstPrompt = remainds.First();

        if (CharCount(firstPrompt) > Limit)
        {
            var separatedFirstPrompt = firstPrompt.Split(" ");

            return CharCount(separatedFirstPrompt.First()) > Limit ? 
                SplitPrompt(prompts.Append(firstPrompt[..Limit]), remainds.Skip(1).Prepend(firstPrompt[Limit..])) :
                SplitSentence(prompts, remainds, separatedFirstPrompt);
        }

        var totalLength = 0;
        var promptLength = remainds.Select(sentence => (Sentence: sentence, TotalLength: Increment(CharCount(sentence) + 1, ref totalLength)));
        var limitedPrompts = promptLength.Where(sentence => sentence.TotalLength - 1 <= Limit).ToArray();

        return SplitPrompt(prompts.Append(string.Join(" ", limitedPrompts.Select(prompt => prompt.Sentence))), remainds.Skip(limitedPrompts.Length));
    }


    private (IEnumerable<string> Prompts, IEnumerable<string> Remainds) SplitSentence(IEnumerable<string> prompts, IEnumerable<string> remainds, IEnumerable<string> words)
    {
        var (allPrompts, _) = SplitPrompt(prompts, words);
        return SplitPrompt(allPrompts.SkipLast(1), remainds.Skip(1).Prepend(allPrompts.Last()));
    }

    private int Increment(int value, ref int target)
    {
        target += value;
        return target;
    }

    private int CharCount(string str) => str.Select(c => Regex.IsMatch(c.ToString(), @"^[\u2E80-\uFE4F]*$") ? 2 : 1).Sum();
}
