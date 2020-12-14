// Copyright (c) 2019 SceneGate

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
namespace Yarhl
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
  #if !NET461
    using System.Runtime.InteropServices;
  #endif
    using System.Runtime.Loader;

    /// <summary>
    /// Utilities to work with Assemblies in different frameworks.
    /// </summary>
    static class AssemblyUtils
    {
        /// <summary>
        /// Load assemblies in different .NET implementations.
        /// </summary>
        /// <param name="paths">List of assemblies to load.</param>
        /// <returns>The assemblies.</returns>
        public static IEnumerable<Assembly> LoadAssemblies(this IEnumerable<string> paths)
        {
          #if NET461
            return LoadAssembliesNetFramework(paths);
          #else
            string framework = RuntimeInformation.FrameworkDescription;
            if (framework.StartsWith(".NET Core", StringComparison.Ordinal)) {
                return LoadAssembliesNetCore(paths);
            } else if (framework.StartsWith(".NET Framework", StringComparison.Ordinal)) {
                return LoadAssembliesNetFramework(paths);
            } else if (framework.StartsWith("Mono", StringComparison.Ordinal)) {
                return LoadAssembliesNetFramework(paths);
            } else {
                // .NET 5.0.0 or later
                return LoadAssembliesNetCore(paths);
            }
          #endif
        }

        /// <summary>
        /// Load assemblies from .NET Core.
        /// </summary>
        /// <remarks>
        /// <para>In .NET Core for some bugs / features we can't use the method
        /// Assembly.LoadFile because two identical types can return false in
        /// an equality. For that reason we need to load the assemblies with
        /// the AssemblyLoadContext which is only available in .NET Core.</para>
        /// </remarks>
        /// <param name="paths">List of assemblies paths.</param>
        /// <returns>The load assemblies.</returns>
        static IEnumerable<Assembly> LoadAssembliesNetCore(
            IEnumerable<string> paths)
        {
            List<Assembly> assemblies = new List<Assembly>();
            foreach (string path in paths) {
                try {
                    Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
                    assemblies.Add(assembly);
                } catch (BadImageFormatException) {
                    // Bad IL. Skip.
                }
            }

            return assemblies;
        }

        /// <summary>
        /// Load assemblies from .NET Framework.
        /// </summary>
        /// <param name="paths">List of assemblies paths.</param>
        /// <returns>The load assemblies.</returns>
        static IEnumerable<Assembly> LoadAssembliesNetFramework(
            IEnumerable<string> paths)
        {
            List<Assembly> assemblies = new List<Assembly>();
            foreach (string path in paths) {
                try {
                    // We try to avoid the non-recommended load from file methods.
                    // Instead we get the full assembly name.
                    // In the future we could realize assembly validations with
                    // public keys, name or version.
                    AssemblyName libName = AssemblyName.GetAssemblyName(path);
                    Assembly library = Assembly.Load(libName);
                    assemblies.Add(library);
                } catch (BadImageFormatException) {
                    // Bad IL. Skip.
                }
            }

            return assemblies;
        }
    }
}