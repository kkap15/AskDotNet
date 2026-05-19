using AskDotNet.Core.Records;
using Markdig;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;
using Microsoft.ML.Tokenizers;

namespace AskDotNet.Ingest;

public class Chunker(int maxTokens = 800, int minTokens = 100)
{
    private readonly TiktokenTokenizer _tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    private sealed record Section(string Heading, List<Block> Blocks);
    
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
        
        var preambleBlocks = RenderBlocks(preamble);
        if (CountTokens(preambleBlocks) >= minTokens)
        {
            chunks.Add(CreateChunk(page, page.Title, preamble));
        }

        foreach (var section in sections)
        {
            //render section blocks to string
            var sectionContent = RenderBlocks(section.Blocks);
            var sectionTokens = CountTokens(sectionContent);
            if (sectionTokens <= maxTokens)
            {
                var chunk = CreateChunk(page, section.Heading, section.Blocks);
                if (IsCleanContent(chunk.Content))
                {
                    chunks.Add(chunk);
                }
            }
            else
            {
                chunks.AddRange(SplitAtH3(page, section));
            }
        }
        
        return chunks;
    }
    
    private int CountTokens(string text) => _tokenizer.CountTokens(text);

    private Chunk CreateChunk(Page page, string sectionHeading, IEnumerable<Block> blocks)
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

    private IEnumerable<Chunk> SplitAtH3(Page page, Section section)
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
    
    private static bool IsCleanContent(string content)
    {
        return !content.Contains('<') && !content.Contains('>');
    }
    
    private static string RenderBlocks(IEnumerable<Block> blocks)
    {
        var writer = new StringWriter();
        var renderer = new NormalizeRenderer(writer);
        foreach (var block in blocks)
        {
            renderer.Render(block);
        }
        
        return writer.ToString().Trim();
    }
}
