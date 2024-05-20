using System;
using Xunit;
using FluentAssertions;
using Brer.Attributes;
using Brer.Exceptions;

namespace BrerTests.Attributes;

public class WildCardHandlerAttributeTest
{
    [Theory]
    [InlineData("topic.#", true)]
    [InlineData("*.topic.*", true)]
    [InlineData("#.topic", true)]
    [InlineData("topic.#.second", true)]
    [InlineData("*.*.topic.#", true)]
    [InlineData("topic.*.#", true)]
    [InlineData("*.#", true)]
    [InlineData("something", false)]
    [InlineData("something.*", true)]
    [InlineData("*.something", true)]
    [InlineData("*", true)]
    [InlineData("#", true)]
    [InlineData("", false)]
    public void WildCardHandlerAttribute_Constructor_Should_Handle_Valid_And_Invalid_Cases(string topicWildCard,
        bool valid)
    {
        // If the topicWildCard is not valid, an InvalidWildCardTopicFormatException is expected
        Action action = () => new WildCardHandlerAttribute(topicWildCard);

        if (valid)
        {
            action.Should().NotThrow<InvalidWildCardTopicFormatException>();
            var attribute = new WildCardHandlerAttribute(topicWildCard);
            attribute.TopicWildCard.Should().Be(topicWildCard);
        }
        else
        {
            action.Should().Throw<InvalidWildCardTopicFormatException>();
        }
    }
}
