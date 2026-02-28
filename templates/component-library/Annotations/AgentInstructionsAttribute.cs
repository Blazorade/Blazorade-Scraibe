using System;
using System.Collections.Generic;
using System.Text;
using {{ComponentLibraryName}}.ShortCodes;

namespace {{ComponentLibraryName}}.Annotations
{
    /// <summary>
    /// An attribute that is used to specify instructions for an AI agent to use when processing a shortcode component.
    /// Based on these instructions the AI agent enterprets the UI hint specified by a content author on the 
    /// <see cref="ShortCodeComponentBase.UIHint"/> and determines what CSS classes to add to the component in the 
    /// <see cref="ShortCodeComponentBase.CssClasses"/> so that they then get rendered as CSS classes on the resulting
    /// HTML element.
    /// </summary>
    /// <remarks>
    /// The attribute is specified on a shortcode component that inherits from <see cref="ShortCodeComponentBase"/>
    /// and it will be use to provide an AI agent with information about what CSS classes the agent needs to add...
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class AgentInstructionsAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the <see cref="AgentInstructionsAttribute"/> class with the specified UI hint.
        /// </summary>
        public AgentInstructionsAttribute(string agentInstructions)
        {
            this.AgentInstructions = agentInstructions;
        }

        /// <summary>
        /// The instructions for an AI agent that guides how the agent should process the component that the attribute
        /// is associated. with.
        /// </summary>
        public string AgentInstructions { get; private set; }
    }
}
