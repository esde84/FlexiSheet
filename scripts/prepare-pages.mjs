import { readFile, writeFile, copyFile } from "node:fs/promises";
import { resolve } from "node:path";
import { gzipSync, brotliCompressSync } from "node:zlib";

const root = resolve("artifacts/publish/wwwroot");
const basePath = process.env.BASE_PATH ?? "/FlexiSheet/";
if (!/^\/(?:[A-Za-z0-9_-]+\/)*$/.test(basePath)) {
    throw new Error("BASE_PATH must be an absolute path ending in /.");
}
const path = resolve(root, "index.html");
const html = await readFile(path, "utf8");
const baseTag = /<base href="\/(?:[A-Za-z0-9_-]+\/)*" \/>/;
if (!baseTag.test(html)) {
    throw new Error("The generated index must contain an absolute base path ending in /.");
}
const prepared = html.replace(baseTag, `<base href="${basePath}" />`);
await Promise.all([
    writeFile(path, prepared),
    writeFile(`${path}.gz`, gzipSync(prepared)),
    writeFile(`${path}.br`, brotliCompressSync(prepared))
]);
await copyFile(path, resolve(root, "404.html"));
await writeFile(resolve(root, ".nojekyll"), "");
