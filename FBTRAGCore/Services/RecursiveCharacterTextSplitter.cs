using System.Text;

namespace FBTRAGCore.Services;

public class RecursiveCharacterTextSplitter
{
    private readonly int _chunkSize;
    private readonly int _overlapSize;
    private readonly string[] _separators = { "\n\n", "\n", " ", "" };

    public RecursiveCharacterTextSplitter(int chunkSize, int overlapSize)
    {
        _chunkSize = chunkSize;
        _overlapSize = overlapSize;
    }

    public IEnumerable<string> Split(string text)
    {
        var chunks = new List<string>();
        SplitTextRecursive(text, _chunkSize, _overlapSize, _separators, 0, chunks);
        return chunks;
    }

    private void SplitTextRecursive(string text, int chunkSize, int overlapSize, string[] separators, int separatorIndex, List<string> chunks)
    {
        if (string.IsNullOrEmpty(text) || separatorIndex >= separators.Length)
        {
            if (!string.IsNullOrEmpty(text))
                chunks.Add(text);
            return;
        }

        var separator = separators[separatorIndex];
        var splits = string.IsNullOrEmpty(separator)
            ? new[] { text }
            : text.Split(new[] { separator }, StringSplitOptions.None);

        var currentChunk = new StringBuilder();
        foreach (var split in splits)
        {
            if (currentChunk.Length + split.Length <= chunkSize)
            {
                currentChunk.Append(split);
            }
            else
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString());
                    // Handle overlap
                    if (overlapSize > 0)
                    {
                        var overlapText = currentChunk.ToString()[^Math.Min(overlapSize, currentChunk.Length)..];
                        currentChunk = new StringBuilder(overlapText);
                    }
                    else
                    {
                        currentChunk = new StringBuilder();
                    }
                }

                if (split.Length <= chunkSize)
                {
                    currentChunk.Append(split);
                }
                else
                {
                    // Try next separator
                    SplitTextRecursive(split, chunkSize, overlapSize, separators, separatorIndex + 1, chunks);
                }
            }
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString());
        }
    }
}