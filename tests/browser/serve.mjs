import http from "node:http";
import { readFile, stat } from "node:fs/promises";
import { resolve, extname, sep } from "node:path";

const root = resolve("artifacts/publish/wwwroot");
const types = {
    ".html": "text/html",
    ".js": "text/javascript",
    ".mjs": "text/javascript",
    ".json": "application/json",
    ".css": "text/css",
    ".wasm": "application/wasm"
};

http.createServer(async (request, response) => {
    const url = new URL(request.url, "http://127.0.0.1");
    if (!url.pathname.startsWith("/FlexiSheet/")) {
        response.writeHead(404).end();
        return;
    }
    const relative = url.pathname.slice("/FlexiSheet/".length);
    let path = resolve(root, relative || "index.html");
    if (!path.startsWith(root + sep)) {
        response.writeHead(404).end();
        return;
    }
    try {
        if (!(await stat(path)).isFile()) {
            response.writeHead(404).end();
            return;
        }
    } catch (error) {
        if (error.code !== "ENOENT") throw error;
        if (extname(path)) {
            response.writeHead(404).end();
            return;
        }
        path = resolve(root, "404.html");
    }
    response.writeHead(200, {
        "Content-Type": types[extname(path)] ?? "application/octet-stream",
        "Cache-Control": "no-store"
    });
    response.end(await readFile(path));
}).listen(5187, "127.0.0.1");
