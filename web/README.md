# Hosting

Two targets, same `build/webgl` output:

- **itch.io** (primary link for posts): `tools/deploy_itch.sh`. Set the project to HTML, viewport 1280×720, "Fullscreen button" on, "Mobile friendly" off.
  itch serves gzip with correct headers, so loads are fast. Put this link in the **first comment** of LinkedIn posts, never the body.
- **GitHub Pages** (source-adjacent link for the README): `tools/deploy_pages.sh`. Pages doesn't set `Content-Encoding` for `.gz`,
  so the build is made with Unity's decompression fallback on; it still works, just a little slower to load.

WebGPU: once the project is on Unity 6.6, `tools/build_webgl.sh --webgpu` puts WebGPU first with WebGL 2 fallback.
WebGPU only initialises over HTTPS, which both hosts provide.

Local check: `tools/serve.sh` then open http://localhost:8080.
