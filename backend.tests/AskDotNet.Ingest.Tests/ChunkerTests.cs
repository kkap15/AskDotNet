using AskDotNet.Core.Records;
using FluentAssertions;

namespace AskDotNet.Ingest.Tests;

public class ChunkerTests
{
    private readonly Chunker _chunker = new Chunker(maxTokens: 800, minTokens: 100);

    private static Page MakePage(string markdown) => new(
        Url: "https://learn.microsoft.com/test",
        Title: "Test Page",
        RawMarkdown: markdown,
        HeadingPath: ["C#", "Fundamentals"],
        FetchedAt: DateTimeOffset.UtcNow
    );
    
    [Fact]
    public void Chunk_EmptyPage_ReturnEmpty()
    {
        var page = MakePage("");
        var chunks = _chunker.Chunk(page);
        
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void Chunk_SingleH2_ProducesOneChunk()
    {
        var markdown = """
                       ## What is a struct

                       A struct is a value type that holds its data directly in the instance.
                       It is used for small, lightweight data containers such as coordinates.
                       Unlike classes, structs are stored on the stack and copied by value.
                       """;
        
        var chunks = _chunker.Chunk(MakePage(markdown));
        
        chunks.Should().HaveCount(1);
        chunks[0].SectionHeading.Should().Be("What is a struct");
        chunks[0].Content.Should().Contain("value type");
        chunks[0].SourceUrl.Should().Be("https://learn.microsoft.com/test");
    }
    
    [Fact]
    public void Chunk_MultipleH2_ProducesMultipleChunks()
    {
        var markdown = """
                       ## Structs

                       A struct is a value type that holds its data directly in the instance.
                       It is used for small lightweight data containers.

                       ## Enums

                       An enum is a value type defined by a set of named constants.
                       It is used to represent a choice from a set of mutually exclusive values.
                       """;

        var chunks = _chunker.Chunk(MakePage(markdown));
        chunks.Should().HaveCount(2);
        chunks[0].SectionHeading.Should().Be("Structs");
        chunks[1].SectionHeading.Should().Be("Enums");
    }

    [Fact]
    public void Chunk_PreambleAboveMinTokens_EmitsPreambleChunk()
    {
        // Use minTokens: 5 so a short preamble still qualifies
        var chunker = new Chunker(maxTokens: 800, minTokens: 5);
        var markdown = """
                                   This is the preamble content before any heading.

                       }
                      It contains enough text to be above the minimum token threshold.
            
            ## First Section
            
            Content of the first section goes here with enough text.
            """;

        var chunks = chunker.Chunk(MakePage(markdown));

        chunks.Should().HaveCountGreaterThanOrEqualTo(2);
        chunks[0].SectionHeading.Should().Be("Test Page");
    }

    [Fact]
    public void Chunk_AllChunksHaveCorrectMetadata()
    {
        var markdown = """
                       ## Overview

                       C# is a modern, object-oriented programming language developed by Microsoft.
                       It runs on the .NET platform and is widely used for building applications.
                       """;

        var chunks = _chunker.Chunk(MakePage(markdown));

        chunks.Should().HaveCount(1);
        chunks[0].SourceUrl.Should().Be("https://learn.microsoft.com/test");
        chunks[0].SourceTitle.Should().Be("Test Page");
        chunks[0].HeadingPath.Should().BeEquivalentTo(["C#", "Fundamentals"]);
        chunks[0].TokenCount.Should().BeGreaterThan(0);
        chunks[0].Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Chunk_EachChunkHasUniqueId()
    {
        var markdown = """
                       ## Section One

                       Content for section one with enough text to be meaningful.

                       ## Section Two

                       Content for section two with enough text to be meaningful.

                       ## Section Three

                       Content for section three with enough text to be meaningful.
                       """;

        var chunks = _chunker.Chunk(MakePage(markdown));

        var ids = chunks.Select(c => c.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }
} 