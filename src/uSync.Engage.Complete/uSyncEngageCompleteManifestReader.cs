using System.Text.Json.Nodes;

using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;

namespace uSync.Engage.Complete;

/// <summary>
///  manifest built in code, so the version always matches the assembly.
/// </summary>
internal class uSyncEngageCompleteManifestReader : IPackageManifestReader
{
    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        PackageManifest manifest = new()
        {
            Id = "uSync.Engage.Complete",
            Name = "uSync.Engage.Complete",
            AllowTelemetry = true,
            Version = typeof(uSyncEngageCompleteManifestReader).Assembly.GetName().Version?.ToString(3) ?? "17.0.0",
            Extensions = [
                new JsonObject {
                    ["type"] = "bundle",
                    ["alias"] = "uSync.Engage.Complete.Bundle",
                    ["name"] = "uSync.Engage.Complete Bundle",
                    ["js"] = "/App_Plugins/uSyncEngageComplete/usync-engage-complete.js",
                }
            ]
        };

        return Task.FromResult<IEnumerable<PackageManifest>>([manifest]);
    }
}
