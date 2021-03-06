// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Completion.FileSystem
{
    internal sealed class GlobalAssemblyCacheCompletionHelper
    {
        private static readonly Lazy<List<string>> s_lazyAssemblySimpleNames =
            new Lazy<List<string>>(() => GlobalAssemblyCache.Instance.GetAssemblySimpleNames().ToList());

        private readonly CompletionItemRules _itemRules;

        public GlobalAssemblyCacheCompletionHelper(CompletionItemRules itemRules)
        {
            Debug.Assert(itemRules != null);
            _itemRules = itemRules;
        }

        public Task<ImmutableArray<CompletionItem>> GetItemsAsync(string directoryPath, CancellationToken cancellationToken)
        {
            return Task.Run(() => GetItems(directoryPath, cancellationToken));
        }

        // internal for testing
        internal ImmutableArray<CompletionItem> GetItems(string directoryPath, CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<CompletionItem>.GetInstance();

            var comma = directoryPath.IndexOf(',');
            if (comma >= 0)
            {
                var partialName = directoryPath.Substring(0, comma);
                foreach (var identity in GetAssemblyIdentities(partialName))
                {
                    result.Add(CommonCompletionItem.Create(identity.GetDisplayName(), glyph: Glyph.Assembly, rules: _itemRules));
                }
            }
            else
            {
                foreach (var displayName in s_lazyAssemblySimpleNames.Value)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    result.Add(CommonCompletionItem.Create(displayName, glyph: Glyph.Assembly, rules: _itemRules));
                }
            }

            return result.ToImmutableAndFree();
        }

        private IEnumerable<AssemblyIdentity> GetAssemblyIdentities(string partialName)
        {
            return IOUtilities.PerformIO(() => GlobalAssemblyCache.Instance.GetAssemblyIdentities(partialName),
                SpecializedCollections.EmptyEnumerable<AssemblyIdentity>());
        }
    }
}
