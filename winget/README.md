# Winget Manifests (For winget-pkgs PRs)

This repo does not control the live Winget package. Winget manifests are hosted in `microsoft/winget-pkgs`.

These files are provided to make updates repeatable for maintainers. To publish a fix:

1. Update/verify the release asset URL and SHA256 in `winget/manifests/.../*.installer.yaml`
2. Copy the manifest folder into a fork of `microsoft/winget-pkgs`
3. Open a PR

## PATH / Terminal Commands

The installer manifest uses a ZIP + nested portable installer:
- `NestedInstallerType: portable`
- `NestedInstallerFiles` with `PortableCommandAlias`

This makes Winget create shims in `%LOCALAPPDATA%\\Microsoft\\WinGet\\Links`, which is on the user PATH in new terminal sessions.

