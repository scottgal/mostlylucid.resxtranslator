using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mostlylucid.ResxTranslator.Core.Models;
using Mostlylucid.ResxTranslator.Core.Services;

namespace Mostlylucid.ResxTranslator.Tests;

public class ResxParserTests
{
    private readonly ResxParser _parser;
    private readonly string _testDirectory;

    public ResxParserTests()
    {
        _parser = new ResxParser(NullLogger<ResxParser>.Instance);
        _testDirectory = Path.Combine(Path.GetTempPath(), "ResxTranslatorTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void ParseFile_Should_Extract_Entries()
    {
        // Arrange
        var resxContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <data name=""HelloWorld"" xml:space=""preserve"">
    <value>Hello, World!</value>
    <comment>Greeting message</comment>
  </data>
  <data name=""GoodbyeWorld"" xml:space=""preserve"">
    <value>Goodbye, World!</value>
  </data>
</root>";

        var filePath = Path.Combine(_testDirectory, "Test.resx");
        File.WriteAllText(filePath, resxContent);

        // Act
        var result = _parser.ParseFile(filePath);

        // Assert
        result.Should().NotBeNull();
        result.Entries.Should().HaveCount(2);

        var entry1 = result.Entries[0];
        entry1.Key.Should().Be("HelloWorld");
        entry1.Value.Should().Be("Hello, World!");
        entry1.Comment.Should().Be("Greeting message");

        var entry2 = result.Entries[1];
        entry2.Key.Should().Be("GoodbyeWorld");
        entry2.Value.Should().Be("Goodbye, World!");
    }

    [Fact]
    public void ParseFile_Should_Detect_Language_From_Filename()
    {
        // Arrange
        var resxContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <data name=""Test"" xml:space=""preserve"">
    <value>Value</value>
  </data>
</root>";

        var filePath = Path.Combine(_testDirectory, "Resources.es.resx");
        File.WriteAllText(filePath, resxContent);

        // Act
        var result = _parser.ParseFile(filePath);

        // Assert
        result.SourceLanguage.Should().Be("es");
        result.BaseName.Should().Be("Resources");
    }

    [Fact]
    public void WriteFile_Should_Create_Valid_Resx()
    {
        // Arrange
        var entries = new List<ResxEntry>
        {
            new() { Key = "Key1", Value = "Value1", Comment = "Comment1" },
            new() { Key = "Key2", Value = "Value2" }
        };

        var outputPath = Path.Combine(_testDirectory, "Output.resx");

        // Act
        _parser.WriteFile(outputPath, entries);

        // Assert
        File.Exists(outputPath).Should().BeTrue();

        var parsed = _parser.ParseFile(outputPath);
        parsed.Entries.Should().HaveCount(2);
        parsed.Entries[0].Key.Should().Be("Key1");
        parsed.Entries[0].Value.Should().Be("Value1");
        parsed.Entries[0].Comment.Should().Be("Comment1");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
