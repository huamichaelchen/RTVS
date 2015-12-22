﻿using System.Collections.Generic;
using Microsoft.R.Support.Help.Definitions;

namespace Microsoft.R.Editor.Completions.Definitions
{
    /// <summary>
    /// Provides information on variables members or
    /// variables declared in a global scope.
    /// </summary>
    public interface IVariablesProvider
    {
        /// <summary>
        /// Given variable name determines number of members
        /// </summary>
        /// <param name="variableName">Variable name or null if global scope</param>
        int GetMemberCount(string variableName);

        /// <summary>
        /// Given variable name returns variable members
        /// adhering to specified criteria. Last member name
        /// may be partial such as abc$def$g
        /// </summary>
        /// <param name="variableName">
        /// Variable name such as abc$def$g. 'g' may be partially typed
        /// in which case providers returns members of 'def' filtered to 'g' prefix.
        /// </param>
        /// <param name="maxCount">Max number of members to return</param>
        IReadOnlyCollection<INamedItemInfo> GetMembers(string variableName, int maxCount);
    }
}
