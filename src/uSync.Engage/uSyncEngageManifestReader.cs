using System.Text.Json.Nodes;

using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace uSync.Engage;

/// <summary>
///  manifest built in code, so the version always matches the assembly.
/// </summary>
internal class uSyncEngageManifestReader : IPackageManifestReader
{
    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        PackageManifest manifest = new()
        {
            Id = "uSync.Engage",
            Name = "uSync.Engage",
            AllowTelemetry = true,
            Version = typeof(uSyncEngageManifestReader).Assembly.GetName().Version?.ToString(3) ?? "17.0.0",
            Extensions = [
                new JsonObject {
                    ["type"] = "localization",
                    ["alias"] = "uSync.Engage.Localization.En",
                    ["name"] = "uSync.Engage English",
                    ["meta"] = new JsonObject { ["culture"] = "en" },
                    ["js"] = "/App_Plugins/uSync.Engage/lang/en.js",
                }
            ]
        };

        return Task.FromResult<IEnumerable<PackageManifest>>([manifest]);
    }
}
