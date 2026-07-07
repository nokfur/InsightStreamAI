using System.Text;

namespace InsightStreamAI.Infrastructure.TextSplitters;

public static class TextChunker
{
    public static List<string> SplitText(string text, int maxChunkSize = 800, int overlapSize = 100)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        if (maxChunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxChunkSize), "Chunk size must be greater than zero.");
        }

        if (overlapSize < 0 || overlapSize >= maxChunkSize)
        {
            throw new ArgumentOutOfRangeException(nameof(overlapSize), "Overlap size must be non-negative and less than the chunk size.");
        }

        var chunks = new List<string>();
        int textLength = text.Length;
        int index = 0;

        while (index < textLength)
        {
            int remaining = textLength - index;
            int length = Math.Min(maxChunkSize, remaining);

            // If we are at the end, just take the rest
            if (length == remaining)
            {
                chunks.Add(text.Substring(index, length).Trim());
                break;
            }

            // Try to find a natural boundary near the end of the chunk
            int chunkEnd = index + length;
            int splitIndex = FindSplitIndex(text, index, chunkEnd);

            int chunkLength = splitIndex - index;
            if (chunkLength <= 0)
            {
                // Fallback: split exactly at maxChunkSize if no boundary was found
                chunkLength = length;
            }

            chunks.Add(text.Substring(index, chunkLength).Trim());

            // Move the index forward, accounting for overlap
            index += chunkLength - overlapSize;
            
            // Safety check to avoid infinite loop
            if (chunkLength <= overlapSize)
            {
                index = splitIndex; // Force advance if progress is too small
            }
        }

        return chunks.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
    }

    private static int FindSplitIndex(string text, int start, int end)
    {
        // Search backwards from the end for paragraph boundaries
        for (int i = end; i > start; i--)
        {
            if (i + 1 < text.Length && text[i] == '\n' && text[i + 1] == '\n')
            {
                return i + 2;
            }
            if (i < text.Length && text[i] == '\n')
            {
                return i + 1;
            }
        }

        // If no paragraphs, search backwards for sentence boundaries (. ! ?)
        for (int i = end; i > start; i--)
        {
            if (i < text.Length && (text[i] == '.' || text[i] == '!' || text[i] == '?'))
            {
                // Include the punctuation and any trailing space
                int j = i + 1;
                while (j < text.Length && char.IsWhiteSpace(text[j]))
                {
                    j++;
                }
                return j;
            }
        }

        // If no sentences, search backwards for space characters (word boundaries)
        for (int i = end; i > start; i--)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                return i + 1;
            }
        }

        // No boundary found, split exactly at the end
        return end;
    }
}
