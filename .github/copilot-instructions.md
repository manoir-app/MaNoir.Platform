# MaNoir.Core - Project Guidelines

## Scope

This repository hosts the platform foundation of MaNoir.

It is allowed to contain:

- the transverse Core of the platform;
- Core public contracts;
- the Core API;
- the Core admin UI;
- the Communication Hub and its contracts.

It must not become the default location for:

- business domain logic;
- cross-domain operational agents;
- composed front experiences;
- platform control-plane and deployment components.

Platform operations components such as deployment orchestration, Kubernetes convergence, Docker runtime management, or control-plane logic belong in a dedicated PlatformOps family, not in this repository.

## Architecture

Before introducing a new component, follow the responsibility model documented in [ARCHITECTURE.md](../ARCHITECTURE.md).

Use these rules:

- Core provides transverse primitives, not hidden business logic.
- Communication Hub ingests, normalizes, correlates, and routes external signals, but does not own final business meaning.
- Business domains own their business truth.
- Platform operations own deployment and runtime control, not business state.
- Agents orchestrate without becoming the source of truth.
- UIs consume public surfaces and must not access internal domain implementation.

## Packaging

This repository follows the multi-repo strategy described in [ARCHITECTURE.md](../ARCHITECTURE.md).

Keep these rules:

- publish `MaNoir.X.Contracts` packages when public contracts are needed;
- publish `MaNoir.X.Client` only when there is a real cross-repo consumption need;
- publish a narrowly scoped technical foundation package only when it supports a stable cross-repo platform concern, for example `MaNoir.Core.AdminUi.Hosting` for admin host bootstrapping;
- publish a shared frontend package under `ui/` only when it carries stable cross-repo React/UI foundations, for example `MaNoir.Core.AdminUi.Kit`;
- keep `Domain`, `Api`, `AdminUi`, and local agent implementations internal by default.

Do not introduce vague shared packages such as `Common`, `Shared`, or `Utils` as a dumping ground.

## Repository Layout

Prefer a stable repository root layout:

- `.github/` for Copilot and repository automation;
- `docs/` for repository-specific documentation;
- `eng/` for build, tooling, and engineering scripts;
- `apps/` for executable or host projects;
- `packages/` for reusable or publishable packages;
- `ui/` for SPA and frontend projects;
- `tests/` for test projects.
- `ops/` for deployment and runtime artifacts when the repo owns them.

Keep these rules:

- every project folder should match the exact project name;
- back-office UI is always named `AdminUi`, never `Bo`, `BackOffice`, `Ui`, or `Pages`;
- do not create competing root folders such as `bo/`, `pages/`, `frontend/`, `backend/`, `api/`, `domain/`, or `services/`;
- if a UI project embeds a frontend app, keep its framework-specific structure inside the project folder, not at repository root.

## Working Style

When adding structure or code:

- prefer reinforcing repo boundaries over shortcutting them;
- reference [README.md](../README.md) and [ARCHITECTURE.md](../ARCHITECTURE.md) instead of duplicating long explanations;
- if a component does not clearly belong here, stop and decide whether it belongs in a domain repo, an agents repo, an experiences repo, or a PlatformOps repo;
- if no placement is clear yet, explicitly propose a quarantine location such as `MaNoir.Lab` instead of silently expanding this repo.