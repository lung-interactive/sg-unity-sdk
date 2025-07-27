using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SGUnitySDK.Editor
{
    // Enums remain the same as they're value types
    public enum FileHost
    {
        S3 = 1
    }

    public enum GameVersionState
    {
        Preparation = 1,
        Released = 2,
        Deprecated = 3
    }

    public enum VersionUpdateType
    {
        Patch = 1,
        Minor = 2,
        Major = 3,
        Specific = 4,
    }

    public enum BuildPlatform
    {
        Windows = 1,
        MacOS = 2,
        Linux = 3,
        Android = 4,
        IOS = 5,
        Web = 6
    }

    [System.Serializable]
    public class VersionDTO
    {
        [JsonProperty("semver")]
        public string Semver;

        [JsonProperty("state")]
        public int State;

        [JsonProperty("is_current")]
        public bool IsCurrent;

        [JsonProperty("is_prerelease")]
        public bool IsPrerelease;

        // [JsonProperty("release_notes")]
        // public ReleaseNotesDTO ReleaseNotes;
    }

    [System.Serializable]
    [JsonConverter(typeof(ReleaseNotesConverter))]
    public class ReleaseNotesDTO
    {
        public string Default;
        public Dictionary<string, string> Localized;

        public string GetForLanguage(string languageCode = "en")
        {
            if (Localized != null && Localized.ContainsKey(languageCode))
                return Localized[languageCode];
            return Default ?? string.Empty;
        }
    }

    public class ReleaseNotesConverter : JsonConverter<ReleaseNotesDTO>
    {
        public override ReleaseNotesDTO ReadJson(JsonReader reader, Type objectType, ReleaseNotesDTO existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var result = new ReleaseNotesDTO();

            if (reader.TokenType == JsonToken.String)
            {
                result.Default = reader.Value.ToString();
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                var obj = JObject.Load(reader);
                result.Localized = obj.ToObject<Dictionary<string, string>>();

                // Assume English as default if available
                if (result.Localized.ContainsKey("en"))
                {
                    result.Default = result.Localized["en"];
                }
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, ReleaseNotesDTO value, JsonSerializer serializer)
        {
            if (value.Localized != null)
            {
                serializer.Serialize(writer, value.Localized);
            }
            else
            {
                writer.WriteValue(value.Default);
            }
        }
    }


    [System.Serializable]
    public class EndVersionDTO
    {
        [JsonProperty("semver")]
        public string Semver;
    }

    [System.Serializable]
    public class PresignedURLDTO
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("file_key")]
        public string FileKey { get; set; }

        [JsonProperty("bucket")]
        public string Bucket { get; set; }

        [JsonProperty("content_type")]
        public string ContentType { get; set; }

        [JsonProperty("size_limit")]
        public int? SizeLimit { get; set; }

        [JsonProperty("checksum")]
        public string Checksum { get; set; }
    }

    // Classes with JsonProperty attributes for snake_case serialization
    [System.Serializable]
    public class GameBuildDTO
    {
        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("platform")]
        public BuildPlatform Platform { get; set; }

        [JsonProperty("src")]
        public string Src { get; set; }

        [JsonProperty("host")]
        public FileHost Host { get; set; }
    }

    [System.Serializable]
    public class SemVerType
    {
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

        [JsonProperty("raw")]
        public string Raw { get; set; }

        [JsonProperty("major")]
        public int Major { get; set; }

        [JsonProperty("minor")]
        public int Minor { get; set; }

        [JsonProperty("patch")]
        public int Patch { get; set; }

        [JsonProperty("prerelease")]
        public string Prerelease { get; set; }

        public static SemVerType From(string version)
        {
            return new SemVerType().LoadFrom(version);
        }

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

        public override string ToString()
        {
            string version = $"{Major}.{Minor}.{Patch}";
            if (!string.IsNullOrEmpty(Prerelease))
            {
                version += $"-{Prerelease}";
            }
            return version;
        }

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

    [System.Serializable]
    public class StartBuildUploadDTO
    {
        [JsonProperty("semver")]
        public string Semver;

        [JsonProperty("platform")]
        public BuildPlatform Platform;

        [JsonProperty("executable_name")]
        public string ExecutableName;

        [JsonProperty("filename")]
        public string Filename;

        [JsonProperty("download_size")]
        public ulong DownloadSize;

        [JsonProperty("installed_size")]
        public ulong InstalledSize;

        [JsonProperty("host")]
        public FileHost Host;

        [JsonProperty("override_existing")]
        public bool? OverrideExisting;
    }

    [System.Serializable]
    public class StartBuildUploadResponseDTO
    {
        [JsonProperty("upload_token")]
        public string UploadToken;

        [JsonProperty("signed_url")]
        public PresignedURLDTO SignedUrl;
    }

    [System.Serializable]
    public class StartGameVersionUpdateDTO
    {
        [JsonProperty("version_update_type")]
        public VersionUpdateType VersionUpdateType;

        [JsonProperty("specific_version")]
        public string SpecificVersion;

        [JsonProperty("is_prerelease")]
        public bool IsPrerelease;

        [JsonProperty("release_notes")]
        public ReleaseNotesDTO ReleaseNotes = new();
    }
}