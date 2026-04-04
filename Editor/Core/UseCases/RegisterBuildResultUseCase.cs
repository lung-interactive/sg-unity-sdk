using System;
using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Editor.Core.Entities;

namespace SGUnitySDK.Editor.Core.UseCases
{
    /// <summary>
    /// Use case for registering the result of a build process.
    /// </summary>
    public class RegisterBuildResultUseCase
    {
        private readonly IBuildRepository _buildRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterBuildResultUseCase"/> class.
        /// </summary>
        /// <param name="buildRepository">Build repository abstraction.</param>
        public RegisterBuildResultUseCase(IBuildRepository buildRepository)
        {
            _buildRepository = buildRepository ??
                throw new ArgumentNullException(nameof(buildRepository));
        }

        /// <summary>
        /// Executes the logic to register a build result.
        /// </summary>
        /// <param name="buildResult">The build result entity to register.</param>
        public void Execute(SGLocalBuildResult buildResult)
        {
            _buildRepository.SaveBuildResult(buildResult);
        }
    }
}
