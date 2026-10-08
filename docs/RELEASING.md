# Releasing

Releases are built by the `release` workflow from a tag. The tag is the only source of the shipped version.

1. Update `CHANGELOG.md`: move the Unreleased entries under a new `## [x.y.z] - yyyy-mm-dd` heading. The workflow refuses a tag without a matching section.
2. Make sure `main` is green and the tag commit is on `main`.
3. Tag and push:

   ```
   git tag v0.1.0
   git push origin v0.1.0
   ```

   Pre-releases use `v0.1.0-beta.1` and are marked as such on GitHub.

4. The workflow publishes `win-x64` and `win-arm64`, zips the portable builds, builds the installers with Inno Setup, writes `SHA256SUMS.txt` and a CycloneDX software bill of materials, attests the build provenance and creates a **draft** release with the changelog section as notes.
5. Review the draft on GitHub and publish it. The hashes are in the release assets; copy them into the notes if you want them visible without a download.

## Signing

The workflow signs the executable and the installer with `eng/sign.ps1` when the `HAS_CERT` repository variable is `true`. The script expects two secrets: `CODE_SIGNING_PFX` (the certificate as base64) and `CODE_SIGNING_PASSWORD`. Until a certificate exists the builds are unsigned and the README explains the SmartScreen warning.

## winget

`distribution/winget/` holds the manifest templates. For the first submission, fill in the version and the SHA-256 of both installers from `SHA256SUMS.txt`, validate with `winget validate`, and submit a pull request to `microsoft/winget-pkgs` (or use `wingetcreate`). Later releases can automate this with `wingetcreate update --submit` from a workflow on `release: published`.

## Versioning

Semantic Versioning. `0.y.z` until the four pillars (monitoring, storage, cleanup, health) are solid on real hardware, then `1.0.0`. `VersionPrefix` in `Directory.Build.props` is the next version and only affects local builds, which are marked `-dev`.
