using System.Collections.Generic;
using SGUnitySDK.Editor.Core.Entities;

namespace SGUnitySDK.Editor.Core.Repositories
{
    /// <summary>
    /// Service abstraction for Unity player build generation.
    /// Encapsulates build orchestration so use cases remain decoupled from
    /// concrete Unity build pipeline implementations.
    /// </summary>
    public interface IBuildGenerationService
    {
        /// <summary>
        /// Generates player builds using the provided setups and output path.
        /// </summary>
        /// <param name="setups">Build setups to execute.</param>
        /// <param name="commonBuildPath">Root output directory for generated artifacts.</param>
        /// <param name="targetVersion">Semantic version to apply to generated builds.</param>
        /// <returns>Collection of local build results for each setup.</returns>
        List<SGLocalBuildResult> GenerateBuilds(
            List<SGBuildSetup> setups,
            string commonBuildPath,
            string targetVersion);
    }
}