using System.Collections.Generic;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Editor.Versioning;

namespace SGUnitySDK.Editor.Infrastructure.UnityAdapters
{
    /// <summary>
    /// Unity-specific implementation of build generation.
    /// Delegates build pipeline execution to SGPlayerBuilder.
    /// </summary>
    public class BuildGenerationService : IBuildGenerationService
    {
        /// <inheritdoc />
        public List<SGLocalBuildResult> GenerateBuilds(
            List<SGBuildSetup> setups,
            string commonBuildPath,
            string targetVersion)
        {
            return SGPlayerBuilder.PerformMultipleBuilds(
                setups,
                commonBuildPath,
                targetVersion);
        }
    }
}