using AskDotNet.Core.Records;
using AskDotNet.Ingest.Helpers;
using AskDotNet.Ingest.Interface;
using AskDotNet.Ingest.Record;
using Markdig;
using Markdig.Syntax;

namespace AskDotNet.Ingest.Utilities;

public class Chunker(int maxTokens = 800, int minTokens = 100) : IChunker
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    
    public IReadOnlyList<Chunk> Chunk(Page page)
    {
        var rawMarkdown = page.RawMarkdown;
        var document = Markdown.Parse(rawMarkdown, _pipeline);
        var chunks = new List<Chunk>();
        //phase1: Collect H2 bucket
        var preamble = new List<Block>();
        var sections = new List<Section>();
        Section? current = null;

        foreach (var block in document)
        {
            if (block is HeadingBlock heading)
            {
                if (heading.Level is 1)
                {
                    continue;
                }

                if (heading.Level is 2)
                {
                    if (current is not null)
                    {
                        sections.Add(current);
                    }
                    var headingText = heading.Inline?.FirstChild?.ToString() ?? string.Empty;
                    current = new Section(headingText, []);
                }
                else
                {
                    if (heading.Level <= 3)
                    {
                        (current?.Blocks ?? preamble).Add(block);
                    }
                }
            }
            else
            {
                (current?.Blocks ?? preamble).Add(block);
            }
        }

        if (current is not null)
        {
            sections.Add(current);
        }
        
        var preambleBlocks = ChunkerHelper.RenderBlocks(preamble);
        if (ChunkerHelper.CountTokens(preambleBlocks) >= minTokens)
        {
            chunks.Add(ChunkerHelper.CreateChunk(page, page.Title, preamble));
        }

        foreach (var section in sections)
        {
            //render section blocks to string
            var sectionContent = ChunkerHelper.RenderBlocks(section.Blocks);
            var sectionTokens = ChunkerHelper.CountTokens(sectionContent);
            if (sectionTokens <= maxTokens)
            {
                var chunk = ChunkerHelper.CreateChunk(page, section.Heading, section.Blocks);
                if (ChunkerHelper.IsCleanContent(chunk.Content))
                {
                    chunks.Add(chunk);
                }
            }
            else
            {
                chunks.AddRange(ChunkerHelper.SplitAtH3(page, section,  minTokens));
            }
        }
        
        return chunks;
    }
}
