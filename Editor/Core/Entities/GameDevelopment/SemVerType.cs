using System;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Semantic versioning (SemVer) type for game versions.
    /// Follows semantic versioning specification: major.minor.patch[-prerelease].
    /// Provides parsing, validation, and increment functionality.
    /// </summary>
    [System.Serializable]
    public class SemVerType
    {
        /// <summary>
        /// Validates whether a version string conforms to semantic versioning.
        /// Format: major.minor.patch[-prerelease]
        /// Example: 1.2.3 or 1.2.3-alpha.1
        /// </summary>
        /// <param name="version">Version string to validate.</param>
        /// <returns>True if version is valid, false otherwise.</returns>
        public static bool SemVerValid(string version)
        {
            try
            {
                var parts = version.Split('.');
                if (parts.Length != 3) return false;

                int.Parse(parts[0]);
                int.Parse(parts[1]);

                var patchParts = parts[2].Split('-');
                int.Parse(patchParts[0]);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Full version string (e.g., "1.2.3-alpha.1").
        /// Automatically reconstructed when version components change.
        /// </summary>
        [JsonProperty("raw")]
        public string Raw { get; set; }

        /// <summary>
        /// Major version component (breaking changes).
        /// </summary>
        [JsonProperty("major")]
        public int Major { get; set; }

        /// <summary>
        /// Minor version component (new features, backward compatible).
        /// </summary>
        [JsonProperty("minor")]
        public int Minor { get; set; }

        /// <summary>
        /// Patch version component (bug fixes).
        /// </summary>
        [JsonProperty("patch")]
        public int Patch { get; set; }

        /// <summary>
        /// Prerelease identifier (e.g., "alpha.1", "beta", "rc.2").
        /// Null or empty for stable releases.
        /// </summary>
        [JsonProperty("prerelease")]
        public string Prerelease { get; set; }

        /// <summary>
        /// Factory method to create SemVerType from a version string.
        /// </summary>
        /// <param name="version">Version string to parse.</param>
        /// <returns>Parsed SemVerType instance.</returns>
        /// <exception cref="Exception">Thrown if version string is invalid.</exception>
        public static SemVerType From(string version)
        {
            return new SemVerType().LoadFrom(version);
        }

        /// <summary>
        /// Parses and loads version information from a version string.
        /// </summary>
        /// <param name="version">Version string to parse.</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="Exception">Thrown if version string is invalid.</exception>
        public SemVerType LoadFrom(string version)
        {
            if (!SemVerValid(version))
            {
                throw new Exception("Invalid semver version");
            }

            this.Raw = version;

            var parts = version.Split('.');
            this.Major = int.Parse(parts[0]);
            this.Minor = int.Parse(parts[1]);

            var patchAndPrerelease = parts[2];
            var prereleaseSplit = patchAndPrerelease.Split('-');
            this.Patch = int.Parse(prereleaseSplit[0]);

            if (prereleaseSplit.Length > 1)
            {
                this.Prerelease = prereleaseSplit[1];
            }

            return this;
        }

        /// <summary>
        /// Returns the version string representation.
        /// Reconstructs Raw from current components.
        /// </summary>
        /// <returns>Version string (e.g., "1.2.3-alpha.1").</returns>
        public override string ToString()
        {
            string version = $"{Major}.{Minor}.{Patch}";
            if (!string.IsNullOrEmpty(Prerelease))
            {
                version += $"-{Prerelease}";
            }
            return version;
        }

        /// <summary>
        /// Increments version according to semantic versioning rules.
        /// Clears prerelease identifier for non-prerelease increments.
        /// </summary>
        /// <param name="type">Increment type (Patch, Minor, Major).</param>
        /// <returns>This instance for method chaining.</returns>
        /// <exception cref="Exception">
        /// Thrown if increment type is invalid or unsupported.
        /// </exception>
        public SemVerType Increment(VersionUpdateType type)
        {
            switch (type)
            {
                case VersionUpdateType.Patch:
                    Patch++;
                    break;
                case VersionUpdateType.Minor:
                    Minor++;
                    Patch = 0;
                    break;
                case VersionUpdateType.Major:
                    Major++;
                    Minor = 0;
                    Patch = 0;
                    break;
                default:
                    throw new Exception($"Invalid version update type: {type}");
            }

            Prerelease = null;
            Raw = ToString();

            return this;
        }
    }
}