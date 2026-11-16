import { defineConfig } from "vite";

export default defineConfig({
  build: {
    lib: {
      // loaded by the bundle in uSyncEngageCompleteManifestReader.cs
      entry: "src/bundle.manifests.ts",
      formats: ["es"],
      fileName: "usync-engage-complete",
    },
    outDir: "../wwwroot/App_Plugins/uSyncEngageComplete",
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco/],
    },
  },
});
