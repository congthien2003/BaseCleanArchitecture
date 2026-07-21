#!/usr/bin/env node

import fs from "node:fs/promises";
import path from "node:path";

const args = process.argv.slice(2);

const getArgValue = (name) => {
  const index = args.indexOf(name);
  if (index === -1) return null;
  return args[index + 1] ?? null;
};

const root = path.resolve(getArgValue("--root") ?? process.cwd());

const dryRun = !args.includes("--yes");
const includeNodeModules = args.includes("--node-modules");
const includeCaches = args.includes("--cache");

const DOTNET_PROJECT_EXTENSIONS = [".csproj", ".fsproj", ".vbproj"];

const SKIP_DIRS = new Set([
  ".git",
  ".svn",
  ".hg",
  ".idea",
  ".vscode",
  "node_modules",
  ".pnpm-store",
  ".yarn",
]);

const targets = new Map();

const exists = async (filePath) => {
  try {
    await fs.access(filePath);
    return true;
  } catch {
    return false;
  }
};

const isDirectory = async (filePath) => {
  try {
    const stat = await fs.lstat(filePath);
    return stat.isDirectory();
  } catch {
    return false;
  }
};

const addTarget = async (folderPath, reason) => {
  if (!(await isDirectory(folderPath))) return;

  const absolutePath = path.resolve(folderPath);
  const relativePath = path.relative(root, absolutePath);

  if (
    !relativePath ||
    relativePath.startsWith("..") ||
    path.isAbsolute(relativePath)
  ) {
    return;
  }

  targets.set(absolutePath, {
    path: absolutePath,
    relativePath,
    reason,
  });
};

const isDotnetProjectFolder = (entries) => {
  return entries.some((entry) => {
    if (!entry.isFile()) return false;

    const extension = path.extname(entry.name).toLowerCase();
    return DOTNET_PROJECT_EXTENSIONS.includes(extension);
  });
};

const readPackageJson = async (packageJsonPath) => {
  try {
    const raw = await fs.readFile(packageJsonPath, "utf8");
    return JSON.parse(raw);
  } catch {
    return null;
  }
};

const isReactProject = (packageJson) => {
  if (!packageJson) return false;

  const deps = {
    ...(packageJson.dependencies ?? {}),
    ...(packageJson.devDependencies ?? {}),
  };

  return Boolean(
    deps.react ||
    deps.vite ||
    deps.next ||
    deps["react-scripts"] ||
    deps["@vitejs/plugin-react"] ||
    deps["@vitejs/plugin-react-swc"],
  );
};

const collectDotnetTargets = async (dir, entries) => {
  if (!isDotnetProjectFolder(entries)) return;

  await addTarget(path.join(dir, "bin"), ".NET build output");
  await addTarget(path.join(dir, "obj"), ".NET intermediate output");
};

const collectReactTargets = async (dir) => {
  const packageJsonPath = path.join(dir, "package.json");

  if (!(await exists(packageJsonPath))) return;

  const packageJson = await readPackageJson(packageJsonPath);

  if (!isReactProject(packageJson)) return;

  const buildFolders = ["dist", "build", ".next", "out"];

  if (includeCaches) {
    buildFolders.push("coverage", ".turbo", ".vite");
  }

  if (includeNodeModules) {
    buildFolders.push("node_modules");
  }

  for (const folderName of buildFolders) {
    await addTarget(path.join(dir, folderName), "React build/cache output");
  }
};

const walk = async (dir) => {
  let entries;

  try {
    entries = await fs.readdir(dir, { withFileTypes: true });
  } catch {
    return;
  }

  await collectDotnetTargets(dir, entries);
  await collectReactTargets(dir);

  for (const entry of entries) {
    if (!entry.isDirectory()) continue;
    if (SKIP_DIRS.has(entry.name)) continue;

    const childPath = path.join(dir, entry.name);

    if (entry.isSymbolicLink?.()) continue;

    await walk(childPath);
  }
};

const removeTargets = async () => {
  const list = [...targets.values()].sort((a, b) =>
    a.relativePath.localeCompare(b.relativePath),
  );

  if (list.length === 0) {
    console.log("No cleanable folders found.");
    return;
  }

  console.log(
    dryRun
      ? "Dry-run mode. Folders that would be removed:"
      : "Removing folders:",
  );

  for (const target of list) {
    console.log(`- ${target.relativePath}  [${target.reason}]`);

    if (!dryRun) {
      await fs.rm(target.path, {
        recursive: true,
        force: true,
        maxRetries: 3,
        retryDelay: 100,
      });
    }
  }

  console.log("");
  console.log(
    dryRun
      ? "Nothing was deleted. Run with --yes to delete these folders."
      : `Done. Removed ${list.length} folder(s).`,
  );
};

await walk(root);
await removeTargets();
