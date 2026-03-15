using System;
using System.Collections.Generic;
using System.Text;

namespace Scraibe.Abstractions.Content
{
    /// <summary>
    /// Produces provider-defined slot markup from prerendered content and placeholder metadata.
    /// </summary>
    public interface ISlotContentProvider
    {
        /// <summary>
        /// Creates the replacement HTML for a slot placeholder.
        /// </summary>
        /// <param name="elementName">The resolved placeholder element name provided as a provider hint.</param>
        /// <param name="innerHtml">The prerendered inner HTML content for the slot.</param>
        /// <param name="placeholderAttributes">The original placeholder attributes as authored in the layout.</param>
        /// <param name="effectiveConfiguration">The effective configuration resolved for the current page.</param>
        /// <returns>HTML containing exactly one root element for slot replacement.</returns>
        string CreateSlotContent(string elementName, string innerHtml, IReadOnlyDictionary<string, string> placeholderAttributes, IReadOnlyDictionary<string, object?> effectiveConfiguration);
    }
}
