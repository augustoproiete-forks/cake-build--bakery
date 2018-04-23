﻿using System;
using System.Collections.Generic;
using Cake.Core.IO;
using Cake.Core.Packaging;
using Cake.Core.Scripting;

namespace Cake.Bakery.Scripting
{
    internal sealed class CachingScriptProcessor : IScriptProcessor
    {
        private readonly IScriptProcessor _processor;
        private readonly IDictionary<PackageReference, IReadOnlyList<FilePath>> _cache;

        public CachingScriptProcessor(IScriptProcessor processor)
        {
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _cache = new Dictionary<PackageReference, IReadOnlyList<FilePath>>(new PackageReferenceComparer());
        }

        public IReadOnlyList<FilePath> InstallAddins(IReadOnlyCollection<PackageReference> addins, DirectoryPath installPath)
        {
            var filePaths = new List<FilePath>();
            foreach (var addin in addins)
            {
                if (addin.Package.Equals("Cake"))
                {
                    continue;
                }

                if (!_cache.TryGetValue(addin, out IReadOnlyList<FilePath> result))
                {
                    result = _processor.InstallAddins(new[] { addin }, installPath);
                    _cache.Add(addin, result);
                }

                if (result != null)
                {
                    filePaths.AddRange(result);
                }
            }
            return filePaths;
        }

        public void InstallModules(IReadOnlyCollection<PackageReference> modules, DirectoryPath installPath)
        {
            // TODO: Figure if we need to do this.
        }

        public void InstallTools(IReadOnlyCollection<PackageReference> tools, DirectoryPath installPath)
        {
            // We don't need to actually install tools with bakery.
        }
    }
}