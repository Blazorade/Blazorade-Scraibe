using Scraibe.Abstractions.Annotation;
using Scraibe.Abstractions.Content;
using System;
using System.Collections.Generic;
using System.Text;

namespace {{ComponentLibraryName}}.Content
{
    [ProviderName("Default")]
    /// <summary>
    /// Default slot provider that wraps prerendered slot content with the chosen root element.
    /// </summary>
    public class DefaultSlotContentProvider : ISlotContentProvider
    {
        /// <summary>
        /// Creates default slot markup by wrapping the provided inner HTML in the selected root element.
        /// </summary>
        /// <param name="elementName">The root element name hint supplied by the composition pipeline.</param>
        /// <param name="innerHtml">The prerendered inner HTML for the slot.</param>
        /// <param name="placeholderAttributes">The placeholder attributes from the layout element.</param>
        /// <param name="effectiveConfiguration">The effective configuration resolved for the current page.</param>
        /// <returns>HTML with a single root element containing the slot inner HTML.</returns>
        public string CreateSlotContent(string elementName, string innerHtml, IReadOnlyDictionary<string, string> placeholderAttributes, IReadOnlyDictionary<string, object?> effectiveConfiguration)
        {
            var builder = new StringBuilder();

            builder
                .Append("<")
                .Append(elementName)
                .AppendLine(">")
                .AppendLine(innerHtml)
                .Append("</")
                .Append(elementName)
                .AppendLine(">")
                ;

            return builder.ToString();
        }
    }
}
