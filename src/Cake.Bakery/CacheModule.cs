﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Cake.Bakery.Scripting;
using Cake.Core;
using Cake.Core.Composition;
using Cake.Core.Scripting;
using Cake.Core.Scripting.Processors.Loading;
using Cake.Scripting.CodeGen;
using IScriptAliasFinder = Cake.Scripting.CodeGen.IScriptAliasFinder;

namespace Cake.Bakery
{
    internal sealed class CacheModule : ICakeModule
    {
        private readonly IScriptAliasFinder _aliasFinder;
        private readonly IScriptProcessor _processor;
        private readonly ICakeEnvironment _environment;
        private readonly ICakeAliasGenerator _aliasGenerator;
        private readonly IEnumerable<ILoadDirectiveProvider> _loadDirectiveProviders;

        public CacheModule(IScriptAliasFinder aliasFinder,
            IScriptProcessor processor,
            ICakeEnvironment environment,
            ICakeAliasGenerator aliasGenerator,
            IEnumerable<ILoadDirectiveProvider> loadDirectiveProviders)
        {
            _aliasFinder = aliasFinder ?? throw new ArgumentNullException(nameof(aliasFinder));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _aliasGenerator = aliasGenerator ?? throw new ArgumentNullException(nameof(aliasGenerator));
            _loadDirectiveProviders = loadDirectiveProviders ?? throw new ArgumentNullException(nameof(loadDirectiveProviders));
        }

        public void Register(ICakeContainerRegistrar registrar)
        {
            if (registrar == null)
            {
                throw new ArgumentNullException(nameof(registrar));
            }

            // Scripting
            var aliasFinder = new CachingScriptAliasFinder(_aliasFinder, _environment);
            registrar.RegisterInstance(aliasFinder)
                .As<IScriptAliasFinder>();
            var processor = new CachingScriptProcessor(_processor);
            registrar.RegisterInstance(processor)
                .As<IScriptProcessor>();
            var aliasGenerator = new CachingCakeAliasGenerator(_aliasGenerator);
            registrar.RegisterInstance(aliasGenerator)
                .As<ICakeAliasGenerator>();
            var loadDirectiveProvider = new CachingLoadDirectiveProvider(_loadDirectiveProviders);
            registrar.RegisterInstance(loadDirectiveProvider)
                .As<ILoadDirectiveProvider>();
        }
    }
}
