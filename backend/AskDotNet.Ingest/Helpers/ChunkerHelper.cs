using AskDotNet.Core.Records;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;
using Microsoft.ML.Tokenizers;

namespace AskDotNet.Ingest.Helpers;

public static class ChunkerHelper
{
    private static readonly TiktokenTokenizer Tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");   
    
    public static IEnumerable<Chunk> SplitAtH3(Page page, Section section, int minTokens)
    {
        var subHeading = section.Heading;
        var subBlocks = new List<Block>();

        foreach (var block in section.Blocks)
        {
            if (block is HeadingBlock { Level: 3 } h)
            {
                if (subBlocks.Count > 0)
                {
                    var content = RenderBlocks(subBlocks);
                    if (CountTokens(content) >= minTokens)
                    {
                        yield return CreateChunk(page, subHeading, subBlocks);
                    }

                    subBlocks = new List<Block>();
                }

                subHeading = $"{section.Heading} > {h.Inline?.FirstChild?.ToString() ?? string.Empty}";
            }
            else
            {
                subBlocks.Add(block);
            }
        }

        if (subBlocks.Count > 0)
        {
            var content = RenderBlocks(subBlocks);
            if (CountTokens(content) >= minTokens)
            {
                yield return CreateChunk(page, subHeading, subBlocks);
            }
        }
    }
    
    public static Chunk CreateChunk(Page page, string sectionHeading, IEnumerable<Block> blocks)
    {
        var content = RenderBlocks(blocks);
        return new Chunk(
            Guid.NewGuid().ToString(),
            page.Url,
            page.Title,
            page.HeadingPath,
            sectionHeading,
            content,
            CountTokens(content)
        );
    }
    
    public static string RenderBlocks(IEnumerable<Block> blocks)
    {
        var writer = new StringWriter();
        var renderer = new NormalizeRenderer(writer);
        foreach (var block in blocks)
        {
            renderer.Render(block);
        }
        
        return writer.ToString().Trim();
    }
    
    public static bool IsCleanContent(string content)
    {
        return !content.Contains('<') && !content.Contains('>');
    }
    
    public static int CountTokens(string text) => Tokenizer.CountTokens(text);
}