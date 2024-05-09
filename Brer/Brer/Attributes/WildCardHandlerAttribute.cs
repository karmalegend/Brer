using System;
using System.Text.RegularExpressions;
using Brer.Exceptions;

namespace Brer.Attributes;

public class WildCardHandlerAttribute : Attribute
{
    public string TopicWildCard { get; set; }
    internal static readonly string RegexHandler = @"^(?=.*[*#])([a-z A-Z 0-9*]+\.)*(#\.)?[a-z A-Z 0-9*+\.]+(\.#)?$";

    public WildCardHandlerAttribute(string topicWildCard)
    {
        if (!Regex.IsMatch(topicWildCard,RegexHandler))
        {
            throw new InvalidWildCardTopicFormatException();
        }

        TopicWildCard = topicWildCard;
    }
    
}
