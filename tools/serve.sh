#!/usr/bin/env bash
# Serves build/webgl with gzip Content-Encoding headers (Unity's decompression fallback also works without them).
REPO="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO/build/webgl" || { echo "no build; run tools/build_webgl.sh"; exit 1; }
PORT="${PORT:-8080}"
python3 - "$PORT" <<'PY'
import sys, http.server, functools
class H(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        p = self.path.split('?')[0]
        if p.endswith('.gz'): self.send_header('Content-Encoding', 'gzip')
        if p.endswith('.br'): self.send_header('Content-Encoding', 'br')
        if p.endswith('.wasm') or p.endswith('.wasm.gz') or p.endswith('.wasm.br'): self.send_header('Content-Type', 'application/wasm')
        if p.endswith('.js.gz') or p.endswith('.js.br'): self.send_header('Content-Type', 'application/javascript')
        self.send_header('Cross-Origin-Opener-Policy', 'same-origin')
        self.send_header('Cross-Origin-Embedder-Policy', 'require-corp')
        super().end_headers()
port = int(sys.argv[1]); print(f"http://localhost:{port}")
http.server.ThreadingHTTPServer(('', port), H).serve_forever()
PY
