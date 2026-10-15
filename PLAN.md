# Pinball Nova — PLAN

## Scope and architecture

A Unity 6.3 LTS (6000.3.x, C#) 3D pinball game. Windows (StandaloneWindows64) is the primary CI/release target; Android and iOS (phones AND tablets, iPad/Android-tablet support enabled) must keep compiling. Same codebase, adaptive to phone, tablet, desktop window sizes.

Layers (assembly definitions keep EditMode tests fast and engine-free where possible):

- `Nova.Core` — pure C# (no UnityEngine): scoring rules, ball-save timer, multiball state machine, mission/lane-chain logic, record-book model + JSON serializer + checksum. Unit-tested in EditMode (and plain `dotnet test` via a thin test project in Docker on Linux).
- `Nova.Physics` — MonoBehaviours around Unity 3D physics: `Flipper`, `Plunger`, `Bumper`, `Slingshot`, `Target`, `Rollover`, `BallController`, substepped fixed timestep with CCD for the ball.
- `Nova.Tables` — table prefabs/scenes (Nova Classic, Nebula Rush) and a `TableDefinition` ScriptableObject per table.
- `Nova.Input` — Unity Input System actions; control schemes: KeyboardMouse, Gamepad, Touch; `InputPromptProvider` for platform-correct glyphs.
- `Nova.Presentation` — VFX, audio, camera rigs, accessibility limiters (flash/shake).
- `Nova.UI` — custom game UI (UI Toolkit), pause/records/settings.
- `Nova.Persistence` — `RecordBookStore` + `SettingsStore` seams over local JSON files (`Application.persistentDataPath`).

## Technology choices

- Unity 6.3 LTS: current supported LTS (6.0 LTS ends support 2026-10-16), Input System, URP, UI Toolkit.
- URP for consistent look across Windows/mobile; bloom/trail effects budgeted for mobile.
- UX strategy: **B — fully custom branded UX**. A bespoke neon-arcade design system that is neither Material nor Cupertino, still honoring each platform: system back/quit/pause conventions, safe areas, window resizing, system text scale, hover/focus states and full keyboard navigation on Windows, platform-correct input prompts (touch vs keyboard/mouse vs gamepad). Rationale: games are visual products; platform-native widget kits would add nothing and fight the art direction.
- Testing/CI: GameCI (`game-ci/unity-test-runner`, `game-ci/unity-builder`) on `windows-latest`; Linux Docker used for structure/lint checks and the pure-C# core test project.
- Fleet identifier prefix: `com.infinityball.pinballnova`.
- No Flutter, React Native, Expo, Kotlin Multiplatform, .NET MAUI, SpriteKit, Flame, or custom engines.

## Milestones and dependency order

1. Skeleton + CI + Git LFS + Unity `.gitignore` (blocks everything).
2. Pure-C# core (scoring, multiball, ball save, record book).
3. Physics core + Nova Classic playable.
4. Adaptive input + custom UI + accessibility.
5. Juice (VFX/audio/camera) + Nebula Rush.
6. Icon wiring + store copy review.
7. Windows release workflow.

## Testing strategy

- EditMode tests for `Nova.Core` (rule tables, state machines, record-book round trip, checksum tamper detection).
- PlayMode smoke tests: ball spawn, flipper hit impulse bounds, drain → ball-save → game-over.
- Input tests: simulate keyboard, gamepad, touch devices via Input System test fixtures; tilt-off fallback.
- Accessibility checks: flash limiter bounds, shake toggle, remap persistence.
- If `UNITY_LICENSE`/`UNITY_EMAIL`/`UNITY_PASSWORD` are not available as repo secrets, mark the Unity job blocked and report it; never fake a green result.
- Verification is host-side in Docker where applicable; never fabricate results.

## Packaging and distribution plan

- `.github/workflows/release.yml`: on `v*` tag or manual dispatch, `windows-latest` GameCI StandaloneWindows64 build → zip → GitHub release asset. Never log secret values.
- Android debug APK and iOS compile verification are CI lanes; Play/TestFlight uploads (using ASC_KEY_ID, ASC_ISSUER_ID, ASC_KEY_P8, ASC_TEAM_ID names only) are later opt-in milestones.
- Identifier `com.infinityball.pinballnova` across Unity bundle ID, Android applicationId, iOS bundle ID, MSIX identity.
- `AppStore/icon.png` (real generated 1024x1024 artwork, present) wired into Windows/Android/iOS icon sets; `AppStore/description.txt` kept accurate.

## Risks

- Unity license secrets may be unavailable in CI → mark Unity jobs blocked and report.
- Pinball physics feel (tunneling, stuck balls) needs substepping/CCD tuning; budget iteration.
- Mobile performance of bloom/trails; provide quality tiers.
- Original-IP discipline: no licensed tables, names, or sounds.

## Explicit non-goals

No licensed tables; no accounts, cloud, ads, IAP, analytics; no VR; no multiplayer; no table editor in MVP; no Flutter, React Native, Expo, Kotlin Multiplatform, .NET MAUI, SpriteKit, Flame, or custom engines; no required motion sensors.
