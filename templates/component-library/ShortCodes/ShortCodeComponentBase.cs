using Blazorade.Core.Components;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace {{ComponentLibraryName}}.ShortCodes
{
    /// <summary>
    /// A base class for short code components. Short code components are components that can be used 
    /// in markdown content. They are rendered as part of the markdown content and can be used to 
    /// add dynamic content to the markdown.
    /// </summary>
    public abstract class ShortCodeComponentBase : BlazoradeComponentBase
    {
        /// <summary>
        /// The ID of the component.
        /// </summary>
        [Parameter]
        public string? Id { get; set; }

        /// <summary>
        /// A space-separated list of CSS classes that should be added to the component when it is rendered. 
        /// This property is used by AI agents to determine which CSS classes to add to the component when 
        /// it is rendered.
        /// </summary>
        [Parameter]
        public string? CssClasses { get; set; }

        /// <summary>
        /// Handles parameters set on the component.
        /// when it is rendered.
        /// </summary>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if(this.Id?.Length > 0)
            {
                this.Attributes["id"] = this.Id;
            }

            if(this.CssClasses?.Length > 0)
            {
                this.AddClasses(this.CssClasses.Split(' '));
            }
        }
    }
}
