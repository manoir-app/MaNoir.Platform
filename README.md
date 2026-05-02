# MaNoir.Platform

[![Build](https://github.com/manoir-app/MaNoir.Platform/actions/workflows/build.yml/badge.svg)](https://github.com/manoir-app/MaNoir.Platform/actions/workflows/build.yml)

MaNoir platform foundation: transverse .NET packages, admin web host, and React UI kit.

## Build And Release

The main GitHub workflow is `build` in `.github/workflows/build.yml`.

### Pull Requests And Manual Runs

- build the .NET solution
- pack NuGet packages as GitHub Actions artifacts
- build and pack the npm kit as a GitHub Actions artifact
- build the Docker images without publishing them

### Push To Main

- automatic versioning using `major.minor.<GITHUB_RUN_NUMBER>`
- NuGet publishing to GitHub Packages
- npm publishing to GitHub Packages
- Docker image publishing to GHCR
- creation of a GitHub prerelease named `build-vX.Y.Z` with attached artifacts

### Stable Promotion

- create a `vX.Y.Z` tag on a commit from `main`
- the workflow republishes the artifacts with that stable version
- a non-prerelease GitHub release is created for that version

This strategy keeps continuous builds on `main`, then promotes a stable version without any additional commit.

## Publishing

### NuGet

- GitHub Packages feed: `https://nuget.pkg.github.com/manoir-app/index.json`
- published packages: `MaNoir.Core`, `MaNoir.Core.Contracts`, `MaNoir.Core.AdminUi.Hosting`

### npm

- package: `@manoir-app/core-admin-ui-kit`
- GitHub Packages registry: `https://npm.pkg.github.com`

Install example:

```bash
npm install @manoir-app/core-admin-ui-kit
```

### Docker

- image: `ghcr.io/manoir-app/manoir-core-adminui`
- image: `ghcr.io/manoir-app/manoir-agents-erza`

## Local Artifacts

- NuGet: `artifacts/nuget`
- npm: `artifacts/npm`