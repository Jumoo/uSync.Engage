import { manifests as actions } from "./menus/manifest.js";

// collates the manifests, the bundle in uSyncEngageCompleteManifestReader.cs loads this.
export const manifests: Array<UmbExtensionManifest> = [...actions];
