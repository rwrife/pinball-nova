# Pinball Nova

Local-first Unity 3D pinball with original tables, juicy modern effects, and honest personal-best records — Windows first, Android/iOS touch ports, no accounts, no cloud, no ads.

One-sentence pitch: arcade-pinball fans on Windows (and phones/tablets) get a physics-honest 3D pinball game with original tables, modern particle/audio juice, and fully local personal-best records, without accounts, cloud leaderboards, ads, or IAP.

## Motivation

Desktop pinball is dominated by licensed-table collections: you pay per table pack, sign into a store ecosystem, and the classic tables are re-skins of the same 1990s layouts. Meanwhile mobile "pinball" is usually a free-to-play grind of timers, energy, and IAP. There is no simple, local-first, physics-honest 3D pinball game you buy-or-build once and just play — on your Windows desktop with keyboard/mouse or gamepad, and on your phone or tablet with touch. Pinball Nova fills that gap with original tables (no licensed IP), an honest personal-best record book, and zero network dependency.

## Target users

- Windows desktop players who want classic-feel pinball with modern presentation and no storefront friction.
- Phone and tablet players who like short (2–5 minute) arcade sessions.
- Record-book players who compete against their own best multiball scores and streaks, not a cloud leaderboard.

## Concrete use cases

- Launch a quick game on the Windows desktop, keyboard flippers (←/→ or Z/·), gamepad supported; full-screen 16:9 or windowed.
- Play the same table on an Android phone or iPad with on-screen flippers plus optional tilt-nudge (always with a non-motion fallback).
- Review the personal-best record book per table: high score, longest multiball, most loops in one ball — all stored locally.
- Tune table realism: flipper strength, ball speed, ball save, camera angle; settings persist locally per platform.

## How to use (intended end-to-end workflow)

1. Install the zipped Windows portable build (or MSIX) from the GitHub release on a `v*` tag.
2. Pick a table → game starts immediately; no account, no first-run sign-in.
3. Score, trigger multiball, chase your personal bests; the record book and options live in the pause menu.
4. Optional later milestone: sideload Android APK / iOS TestFlight builds from CI artifacts.

## MVP feature list

- Unity 6.3 LTS project (6000.3.x), C#, physics-based 3D pinball: flippers, plunger, slingshots, bumpers, targets, rollovers, ball save, multiball (up to 3 balls).
- Two original tables at MVP (one short "Nova Classic" 2-minute layout, one "Nebula Rush" with lane chains and a multiball wizard mode).
- Modern juice: particle bursts, hit flash, screen shake (togglable), ball trails, dynamic table lighting, arcade SFX + adaptive music bed.
- Input: Unity Input System — keyboard/mouse and gamepad on Windows; touch flippers (left/right half-screen and on-screen button fallback) on mobile; optional tilt nudge with a non-motion button/keyboard fallback. Tilt is never required.
- Honest local record book: per-table high scores, longest multiball, session stats; JSON on disk via a `RecordBookStore` seam; tamper-evident (checksum), no cloud.
- Accessibility: screen shake toggle, flash-intensity limiter, colorblind-safe target palettes, remappable keys, scalable UI text, full keyboard navigation on Windows.
- Platform-correct UX: custom game UI (strategy B) with platform-correct input prompts (touch vs keyboard/mouse vs gamepad glyphs) and platform-correct back/quit/pause behavior.

## Non-goals

- No licensed or real-world brand tables; all tables are original IP — no Microsoft 3D Pinball names, assets, or table copies.
- No accounts, cloud leaderboards, analytics, ads, IAP, or any required network access.
- No Flutter, React Native, Expo, Kotlin Multiplatform, .NET MAUI, SpriteKit, Flame, or custom engines — games in this fleet are Unity only.
- No VR, no multiplayer, no table editor in MVP.
- No fabricated completion claims: CI results must be real GameCI output; if Unity license secrets are absent, the build job is marked blocked and reported, never faked.

## Privacy, permissions, and data storage

- 100% offline gameplay. Zero telemetry, zero analytics SDKs.
- All user data (records, settings) stored locally in the per-user save folder; export/import via a plain JSON file; deleting the folder resets the game.
- Permissions kept minimal on mobile (none beyond display); motion sensors are optional and only enabled if the user turns tilt on.

## Current status and milestones

**Status: scaffold only — documentation and planning exist; no Unity project, code, builds, or tests exist yet.** Nothing below is complete.

1. M0 (this repo): planning docs, backlog, CI design. ← here
2. M1: Unity project skeleton + Git LFS + Linux-side structure checks + Windows GameCI EditMode-test lane (or blocked-and-reported if license secrets absent).
3. M2: physics core (flippers, plunger, bumpers, ball save) + Nova Classic table playable on Windows.
4. M3: record book + pause menu + settings/accessibility.
5. M4: juice pass (particles, trails, audio, camera).
6. M5: Nebula Rush table + wizard mode.
7. M6: adaptive input prompts + mobile touch controls compiling (Android debug + iOS compile).
8. M7: Windows release pipeline (`.github/workflows/release.yml` on `v*` → zipped portable build on GitHub release); store submissions deferred.

## Development / build quickstart

- Engine: Unity 6.3 LTS (6000.3.x), pinned via `ProjectVersion.txt` (exact revision committed by the skeleton issue once the editor generates it; the pin is 6000.3).
- Package ID / bundle ID / applicationId / MSIX identity: `com.infinityball.pinballnova` (App Store Connect status: CREATED).
- App Store Connect Actions secrets configured (names only): ASC_KEY_ID, ASC_ISSUER_ID, ASC_KEY_P8, ASC_TEAM_ID. Unity license secret NAMES (never values): UNITY_LICENSE, UNITY_EMAIL, UNITY_PASSWORD.
- Open the repo in Unity Hub after issue #1 creates `Assets/`, `ProjectSettings/`, `Packages/manifest.json`, `ProjectVersion.txt`.
- Primary build target: StandaloneWindows64. Mobile targets Android + iOS (tablets enabled) must keep compiling; store submission is a later milestone.
- Icon: `AppStore/icon.png` (real generated artwork) — issue backlog wires it into Windows `.ico`, Android launcher, and iOS icon sets via Unity Player Settings.
- Store copy lives in `AppStore/description.txt` and must stay accurate to implemented features.

## Identifier

`com.infinityball.pinballnova` is used for the Unity package ID, Android applicationId, iOS bundle identifier, and Windows/MSIX identity name. Never introduce another prefix.

## License

MIT — see LICENSE.
