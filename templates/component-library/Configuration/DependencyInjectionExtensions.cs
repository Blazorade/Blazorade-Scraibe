using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace {{ComponentLibraryName}}.Configuration
{
    /// <summary>
    /// Extension methods related to Dependency Injection (DI).
    /// </summary>
    public static class DependencyInjectionExtensions
    {

        /// <summary>
        /// Registers services required by your solution.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection Register{{ComponentLibraryServiceRegistrationMethod}}(this IServiceCollection services)
        {
            // TODO: If your shortcode components or other Blazor components have services dependencies, this is the
            // place where you can register them on the service collection passed to this method.

            return services;
        }
    }
}
