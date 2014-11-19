// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Framework.Runtime;
using NuGet;

namespace DependencyAnalyzer.Util
{
    public class DependencyFinder
    {
        private const string LibraryTypeProject = "Project";

        private readonly string                _appbasePath;
        private readonly string                _assemblyFolder;
        private readonly ICache                _cache;
        private readonly ICacheContextAccessor _accessor;

        public DependencyFinder(ICacheContextAccessor cacheContextAccessor, ICache cache, IApplicationEnvironment environment, string assemblyFolder)
        {
            _accessor = cacheContextAccessor;
            _cache = cache;
            _assemblyFolder = assemblyFolder;
            _appbasePath = environment.ApplicationBasePath;
        }

        public HashSet<string> GetContractDependencies(string projectName)
        {
            var usedAssemblies = new HashSet<string>();

            var projectFolder = Path.Combine(_appbasePath, projectName);

            // TODO: hardcoded?
            var framework = VersionUtility.ParseFrameworkName("aspnetcore50");

            var hostContext = new ApplicationHostContext(
                                serviceProvider: null,
                                projectDirectory: projectFolder,
                                packagesDirectory: null,
                                configuration: "Debug",     // TODO: hardcoded?
                                targetFramework: framework,
                                cache: _cache,
                                cacheContextAccessor: _accessor,
                                namedCacheDependencyProvider: new NamedCacheDependencyProvider());

            hostContext.DependencyWalker.Walk(hostContext.Project.Name, hostContext.Project.Version, framework);

            var libManager = (ILibraryManager)hostContext.ServiceProvider.GetService(typeof(ILibraryManager));

            foreach (var library in libManager.GetLibraries())
            {
                var isProject = string.Equals(LibraryTypeProject, library.Type, StringComparison.OrdinalIgnoreCase);

                foreach (var loadableAssembly in library.LoadableAssemblies)
                {
                    if (!isProject || !string.Equals(library.Name, loadableAssembly.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        usedAssemblies.Add(loadableAssembly.Name);
                    }

                    PackageAssembly assembly;
                    if (hostContext.NuGetDependencyProvider.PackageAssemblyLookup.TryGetValue(loadableAssembly.Name, out assembly))
                    {
                        usedAssemblies.AddRange(WalkAll(assembly.Path));
                    }
                }
            }

            return usedAssemblies;
        }

        private IList<string> WalkAll(string rootPath)
        {
            var result = new HashSet<string>();
            var stack = new Stack<string>();

            stack.Push(rootPath);
            while (stack.Count > 0)
            {
                var path = stack.Pop();

                if (!result.Add(Path.GetFileNameWithoutExtension(path)))
                {
                    continue;
                }

                foreach (var reference in PEFileHelper.GetReferences(path))
                {
                    var newPath = Path.Combine(_assemblyFolder, reference + ".dll");

                    if (!File.Exists(newPath))
                    {
                        continue;
                    }

                    stack.Push(newPath);
                }
            }

            return result.ToList();
        }
    }
}