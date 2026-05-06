---
name: release
description: Tag and publish a new GitHub release, triggering the publish workflow (builds win-x64 binary and submits a winget update)
user-invocable: true
---

# Release Skill

Creates a new version tag, pushes it, and publishes a GitHub release. This triggers:
- `publish.yaml` — builds a self-contained `win-x64` zip + raw `port.exe`, uploads as release assets
- `winget.yml` — submits an update PR to `microsoft/winget-pkgs` for `kevinboss.port`

## 1. Validate preconditions
Run these in parallel:
- `git status` — working tree must be clean
- `git rev-parse --abbrev-ref HEAD` — must be on `master`
- `git fetch origin && git status -sb` — must be up to date with `origin/master`
- `gh secret list --repo kevinboss/port` — confirm `WINGET_TOKEN` is present
- `git tag --list 'v*' --sort=-v:refname | head -5` — show recent versions to help pick next

If any check fails, stop and report — do not create the tag.

## 2. Determine the version
- If the user passed a version as an argument (e.g. `/release v2.6.0` or `/release 2.6.0`), use it
- Otherwise, propose the next version based on the latest tag and the nature of commits since then (patch vs minor vs major)
- Tag format must be `vMAJOR.MINOR.PATCH` (SemVer with `v` prefix)
- Refuse to reuse an existing tag

## 3. Create and push the tag
```bash
git tag v<version>
git push origin v<version>
```

Do NOT amend or move an existing tag — always create a new version.

## 4. Create the GitHub release
```bash
gh release create v<version> --title "v<version>" --generate-notes
```

This marks the release as `published`, which triggers `publish.yaml`. Once that workflow finishes (status → `released`), `winget.yml` fires and opens the winget PR.

## 5. Monitor the workflow
```bash
gh run watch --repo kevinboss/port
```
Or poll with `gh run list --workflow publish.yaml --limit 1`.

Report back:
- Release URL: `https://github.com/kevinboss/port/releases/tag/v<version>`
- Expected assets: `port-v<version>-win-x64.zip` and `port.exe`
- Winget package URL: `https://winget.run/pkg/kevinboss/port`

## Important
- Never force-push a tag
- If the workflow fails, diagnose and either re-run the failed job or, if the tag is wrong, delete and recreate rather than amend
- Deleting a published release on GitHub is recoverable; deleting a pushed git tag requires `git push origin --delete v<version>` — only do this if explicitly asked
- Winget PRs are reviewed by Microsoft; failures there usually require either a fix in metadata (handled by the action) or a new release
