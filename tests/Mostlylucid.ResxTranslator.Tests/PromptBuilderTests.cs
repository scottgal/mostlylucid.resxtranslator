using FluentAssertions;
using Mostlyucid.LlmBackend.Interfaces;
using Mostlyucid.LlmBackend.Services;

namespace Mostlylucid.ResxTranslator.Tests;

public class PromptBuilderTests
{
    [Fact]
    public void DefaultPromptBuilder_Should_Build_With_Context()
    {
        // Arrange
        var builder = new DefaultPromptBuilder();
        var context = new PromptContext
        {
            SystemMessage = "You are a translator",
            ContextVariables = new Dictionary<string, string>
            {
                ["Language"] = "Spanish",
                ["Style"] = "Formal"
            }
        };

        // Act
        var prompt = builder.BuildPrompt("Translate this text", context);

        // Assert
        prompt.Should().Contain("You are a translator");
        prompt.Should().Contain("Language: Spanish");
        prompt.Should().Contain("Style: Formal");
        prompt.Should().Contain("Translate this text");
    }

    [Fact]
    public void TranslationPromptBuilder_Should_Include_Formatting_Instructions()
    {
        // Arrange
        var builder = new TranslationPromptBuilder();
        var context = new PromptContext
        {
            ContextVariables = new Dictionary<string, string>
            {
                ["SourceLanguage"] = "en",
                ["TargetLanguage"] = "es",
                ["PreserveFormatting"] = "true"
            }
        };

        // Act
        var prompt = builder.BuildPrompt("Hello {0}", context);

        // Assert
        prompt.Should().Contain("en");
        prompt.Should().Contain("es");
        prompt.Should().Contain("placeholders");
        prompt.Should().Contain("Hello {0}");
    }

    [Fact]
    public void PromptBuilder_Should_Replace_Template_Variables()
    {
        // Arrange
        var builder = new DefaultPromptBuilder();
        var context = new PromptContext
        {
            TemplateVariables = new Dictionary<string, string>
            {
                ["name"] = "John",
                ["age"] = "30"
            }
        };

        // Act
        var prompt = builder.BuildPrompt("Hello {{name}}, you are {{age}} years old", context);

        // Assert
        prompt.Should().Contain("Hello John");
        prompt.Should().Contain("you are 30 years old");
    }

    [Fact]
    public void PromptBuilder_Should_Store_And_Retrieve_Memory()
    {
        // Arrange
        var builder = new DefaultPromptBuilder();

        // Act
        builder.AddToMemory("user", "Hello");
        builder.AddToMemory("assistant", "Hi there!");
        var memory = builder.GetMemory();

        // Assert
        memory.Should().HaveCount(2);
        memory[0].Role.Should().Be("user");
        memory[0].Content.Should().Be("Hello");
        memory[1].Role.Should().Be("assistant");
        memory[1].Content.Should().Be("Hi there!");
    }
}
