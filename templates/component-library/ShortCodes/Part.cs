using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace {{ComponentLibraryName}}.ShortCodes
{

    /// <summary>
    /// Represents a content part shortcode component that you use to map parts of your content to parts defined in a page layout.
    /// </summary>
    public class Part : ShortCodeComponentBase
    {

        /// <summary>
        /// The name of the content part. In order for the content part to render, it must have a corresponding content part in the page layout.
        /// </summary>
        [Parameter]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The name of the element to use when rendering the content part into the static HTML that is published on the site.
        /// </summary>
        [Parameter]
        public string ElementName { get; set; } = string.Empty;


        /// <inheritdoc/>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            this.Name = this.Name?.Length > 0 ? this.Name.ToLower() : string.Empty;
            this.ElementName = this.ElementName?.Length > 0 ? this.ElementName.ToLower() : string.Empty;

            if (string.IsNullOrEmpty(this.ElementName))
            {
                switch(this.Name)
                {
                    case "header":
                        this.ElementName = "header";
                        break;

                    case "nav":
                        this.ElementName = "nav";
                        break;

                    case "main":
                        this.ElementName = "main";
                        break;

                    case "footer":
                        this.ElementName = "footer";
                        break;

                    default:
                        this.ElementName = "aside";
                        break;
                }
            }
        }
    }
}
