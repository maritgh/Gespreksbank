# Project Overview
- Game Title: Tussenruimte (CozyRooms project).
- High-Level Concept: A cozy VR meeting place where players join the same online game, explore six themed conversation rooms, and share a conversation with one other person per room.
- Players: Online multiplayer; retain the existing 12-player game capacity and two-person conversation-room limit.
- Inspiration / Reference Games: Existing Tussenruimte environment and Unity VR Multiplayer template; no new reference game required.
- Tone / Art Direction: Preserve the existing warm, calm environment and Dutch interface.
- Target Platform: Quest 3, standalone Android.
- Screen Orientation / Resolution: Headset stereoscopic rendering; preserve current XR resolution settings and use readable world-space UI rather than phone-oriented layouts.
- Render Pipeline: Existing URP-Performant.
- Customization extension selected by the user: three cozy stylized human looks, using the current tracked head-and-hands presentation rather than introducing a full-body avatar. Players choose a display name and visual preset. Do not persist either choice across app launches.

# Game Mechanics
## Core Gameplay Loop
Create a public Tussenruimte game or select an available game from a list. Friends can also join through the shared invitation code. Once connected, both players enter the same environment and see the same avatars, room occupancy, and conversation state. A six-room overview shows empty rooms, rooms with one available seat, and full rooms; players choose a room and enter it using the existing room directory logic.

Before joining, players can open Mijn karakter, enter a name with the existing VR keyboard, and choose one of three distinct human looks. A preview shows the selection before applying it. Names and appearance are visible to other players, including people joining later. Players can reopen the customization menu while connected. Choices remain available during the current app run and reconnects, but reset on the next app launch; no PlayerPrefs, disk files, or cloud profile persistence will be introduced.

The three looks should differ through hairstyle silhouette and upper-body clothing details, not merely color. Proposed directions for visual review: a short-haired person in a cozy sweater, a curly-haired person in a cardigan, and a longer-haired person in a soft collared outfit. Match the existing friendly stylized human head and hands. These are concept directions, not pre-approved finished character meshes; review the three previews before final visual authoring.

User selections: public game list plus join codes; include availability for the six conversation rooms inside the connected game. Public games can be joined by strangers. Preserve the existing two-person room rules rather than treating each physical room as a separate network game.

## Controls and Input Methods
Keep the active New Input System and existing XR controllers, ray interaction, world-space buttons, and join-code keyboard. Do not replace the VR rig or introduce legacy input. Disable duplicate create/join requests while connecting. Leaving a game precedes joining a different one; do not silently disconnect a player because they clicked a browser row.

# UI
Use the project's existing Canvas-based UI, CozyUiFactory styling, and Dutch labels. Extend the hall interface with a separate readable browser panel instead of crowding the existing welcome panel.

```text
Tussenruimte
[Maak een plek] [Beschikbare plekken] [Join met code]
Status: offline / verbinden / verbonden / fout
Uitnodigingscode: ABC123 (when connected)

Beschikbare plekken                     [Vernieuwen]
Tussenruimte · speler A       3 / 12     [Aansluiten]
Tussenruimte · speler B       8 / 12     [Aansluiten]
[Vorige]                      [Volgende]
Loading / no open games / connection error + retry

Kamers in deze plek (live state when connected)
Filmhoek          0 / 2 · Vrij          [Kom binnen]
Droomkamer        1 / 2 · Plek voor jou [Kom binnen]
Kleine gelukjes   2 / 2 · Vol            [disabled]
...remaining three rooms...
[Verlaat plek]
```

Offline conversation-room previews must be explicitly labelled as previews, not live availability. Show real occupancy only from the connected game's synchronized directory. Room entry buttons share the same authority and capacity checks as the existing door buttons. Preserve microphone and seated interaction controls.

## Character customization UI extension
Add Mijn karakter to the existing welcome menu. Use an exclusive menu view in the same hall interface, not an overlapping panel. Provide a clearly visible player name and three named preset cards with preview thumbnails; use a dedicated preview camera that renders only to its RenderTexture, with stereo/XR rendering disabled. Do not redirect the tracked gameplay camera. Reuse the current controller-ray keyboard for name entry.

```text
Mijn karakter
Naam: [chosen name]                  [Naam wijzigen]
[Short hair + sweater] [Curls + cardigan] [Long hair + collared outfit]
              [Preview of selected human look]
[Toepassen]                                      [Terug]
Changes are shared with other players in this game.
```

Names are display names, not unique accounts. Allow duplicate names; never use them as network identities. Trim and normalize input, reject empty/control-only input, and limit to 24 Unicode scalar values while enforcing the actual UTF-8 capacity of the existing FixedString128Bytes field. Display names as literal text in customization, name tags, and player lists. Pending keyboard input is a draft; cancel does not change the current name.

# Key Asset & Context
## Original baseline before the completed room-browser implementation
- `Assets/CozyRooms/Runtime/Networking/CozyTemplateSessionBridge.cs`: reflection bridge to the existing template manager; `CreateSession()` currently calls `CreateNewLobby("Tussenruimte", true, 12)`, making every created game private. Existing join-code support calls `JoinLobbyByCode`; no browser exists here.
- `Assets/VRMPAssets/Scripts/Network/NetworkManagers/XRINetworkGameManager.cs`: public `JoinLobbySpecific(ISessionInfo)` and `CreateNewLobby(...)` already own the network connection flow.
- `Assets/VRMPAssets/Scripts/Network/NetworkManagers/SessionManager.cs`: uses installed multiplayer package, distributed-authority networking, query/create/join APIs, and scene/build metadata. Current query filters and compatibility checks do not enforce compatibility. Keep this network architecture.
- `Assets/CozyRooms/Runtime/UI/CozyRoomUI.cs`: builds world-space welcome and door panels; already reads synchronized two-person room occupancy.
- `Assets/CozyRooms/Runtime/UI/CozyUiFactory.cs`: reuse layout, buttons, text, and XR event-system setup.
- `Assets/CozyRooms/Runtime/Networking/CozyRoomDirectory.cs`: existing shared conversation-room authority and state; inspect its spawn/ownership and disconnect cleanup before wiring the overview.
- `Assets/CozyRooms/Runtime/CozyRooms.Runtime.asmdef`: currently does not reference multiplayer/authentication assemblies; verify their installed assembly names before adding relevant references.
- Installed versions verified in preceding investigation: multiplayer 2.4.1, NGO 2.13.3, Unity 6000.6.9f1. Read installed source for any additional API details rather than changing packages.
- `Assets/XR/Settings/OpenXR Package Settings.asset`: Meta Quest currently forces removal of INTERNET permission. This is an online connectivity blocker to repair, with final APK manifest verification.
- Latest read-only inspection: SampleScene is open, and both Tussenruimte and SampleScene are enabled for builds. Restore Tussenruimte as the only enabled game scene without deleting SampleScene.
- A cloud project is linked, but service activation, permissions, and two-device connectivity are not yet verified.

## Planned assets
- Modify the bridge and UI scripts above.
- Add `Assets/CozyRooms/Runtime/Networking/CozySessionBrowser.cs` for typed discovery, safe result snapshots, refresh, compatibility filtering, and pagination.
- Add verified multiplayer/authentication assembly references to the runtime asmdef as required.
- Minimally adjust template SessionManager metadata visibility/indexing only if needed for reliable discovery; do not replace its connection pipeline or edit package source.
- Modify Quest settings to retain INTERNET permission and build settings to select only Tussenruimte.
- Extend existing Editor tests under `Assets/CozyRooms/Tests/Editor/` with browser filtering/state and room-overview tests. Inspect existing test assembly and conventions first.
- Preserve app identifier `com.cozyrooms.tussenruimte`, the XR tracking setup, lobby spawn at (120, 0, -2), and corrected reset boundary 195.

## Current verified baseline and customization context
- The public game browser and six-room overview have already been implemented. Prior verification recorded 55 passing Editor assertions, a stable lobby and working UI lifecycle, and a live cloud test that created a public game, discovered it, entered an authority-backed room, and left cleanly. The room-browser APK built successfully. Two-headset acceptance and headset visual inspection remain separate tests.
- `Assets/VRMPAssets/Scripts/Player/PlayerAppearanceMenu.cs` already updates the template's LocalPlayerName and LocalPlayerColor. Reuse those established name/color pathways rather than adding a second name synchronization system.
- `Assets/VRMPAssets/Scripts/Network/NetworkPlayer/XRINetworkPlayer.cs` already replicates owner-written name and color values, initializes them on local spawn, and subscribes to subsequent local changes. Add only the new preset-selection state.
- `Assets/VRMPAssets/Scripts/Network/NetworkPlayer/XRAvatarVisuals.cs` assumes a specific head renderer, mouth blend shape, material slot, and local-player material swap. A new preset renderer must not blindly replace those references or inherit incompatible material-slot assumptions.
- `Assets/VRMPAssets/Scripts/UI/OfflineMenu.cs` resets the local name to Unity Creator and assigns a random color in Awake. Coordinate initialization so it cannot overwrite this run's chosen profile during reconnects or scene setup.
- `Assets/VRMPAssets/Prefabs/PlayerPrefabs/XRI_Network_Player_Avatar.prefab` currently has a head mesh with two bones and tracked hand renderers; no Animator/Humanoid Avatar or full-body skeleton was found. Preserve this existing VR presentation. Do not auto-rig a full humanoid, generate locomotion animations, or add a new player controller.
- `Assets/VRMPAssets/Scripts/Player/OfflinePlayerAvatar.cs` supplies offline preview visuals; keep offline and online appearances consistent.
- `Assets/CozyRooms/Runtime/UI/CozyKeyboard.cs` supports controller-ray text input, scalar limits, an initial value, submit callbacks, and cancellation. Extend its input-purpose handling only if name entry requires it; do not break codes or custom questions.

## Additional assets for character customization
- Add `Assets/CozyRooms/Runtime/Player/CozyPlayerProfile.cs`: current-run name and preset state, validation, explicit initialization, and change events; no persistent storage.
- Add `Assets/CozyRooms/Runtime/Player/CozyAvatarStyle.cs`: owner-write preset ID replication and application to all observed avatars, including late joiners. Verify installed NGO behavior and DA ownership before choosing attributes/permissions.
- Add `Assets/CozyRooms/Generated/Avatars/` for a three-entry preset catalog and lightweight modular hairstyle/clothing visuals compatible with the existing head and hand anchors. Preserve original source meshes and prefab assets. Any reference-driven mesh generation, if later needed, requires separate reference approval; do not generate unapproved human meshes as a fallback.
- Add a Tussenruimte-specific network-avatar prefab variant and matching offline/preview visuals as needed. Update the Tussenruimte manager's player-prefab reference and network-prefab registration without replacing the original template avatar in unrelated scenes.
- Modify `CozyRoomUI.cs` for the new menu and its preview lifecycle; modify `CozyKeyboard.cs` only for verified name-entry needs.
- Minimally modify template profile initialization and name rendering components if required to prevent resets or rich-text interpretation. Inspect affected prefabs and script dependents before each edit.
- Add customization tests alongside the existing room-browser tests. A new network behavior changes avatar serialization: all players must install the same updated build; validate compatibility metadata rather than letting mismatched old/new avatars join silently.

# Implementation Steps
1. **Preflight and impact analysis.** Inspect target scene wiring, existing directory spawn/authority, input references, authentication lifecycle, and installed discovery types. Analyze dependencies for every asset before edits. Protect unsaved changes in the currently open SampleScene. Confirm cloud project/environment and online transport configuration; report unavailable cloud prerequisites rather than fabricating available rooms. Dependencies: none.
2. **Fix online/build prerequisites.** Retain Android INTERNET permission by disabling the existing force-removal setting, preserving other Quest features. Keep ARM64/IL2CPP and the current app ID. Restore only Tussenruimte in the enabled build list and inspect profile overrides. Dependencies: Step 1.
3. **Enable public game creation and reliable joining.** Change bridge creation to public, with a distinguishable Tussenruimte display name while preserving 12-player capacity. Retain code joining; add selection-based joining through the template's existing `JoinLobbySpecific` API. Use template authentication and actual connection/failure events to gate actions and clear busy state; avoid relying solely on a 35-second timer. Prevent parallel requests and ensure handlers unsubscribe on teardown. Dependencies: Steps 1–2.
4. **Implement discovery.** Use installed `MultiplayerService.Instance.QuerySessionsAsync` and `ISessionInfo`; do not create a parallel network manager. Query public, unlocked games with free slots, then filter to compatible Tussenruimte scene/build metadata. Verify existing metadata visibility; if inaccessible, make the relevant compatibility properties publicly queryable through supported options in SessionManager. Do not assume session `Type` is a server-side filter. Add loading/error/empty states, refresh throttling, pagination, stale-response protection, and stale/full-room join handling. Keep a controlled refresh while browser is visible and stop it on disable/leave. Dependencies: Steps 1 and 3.
5. **Wire world-space menus.** Extend CozyRoomUI with public-game rows, player counts, refresh/paging buttons, and join controls. Reuse CozyUiFactory and the existing keyboard. Add a six-room overview bound to Directory.StateChanged with occupancy, current-room indicator, and correctly disabled full-room buttons. Refresh views from actual state, not fabricated demo results. Dependencies: Steps 3–4.
6. **Verify shared world behavior.** Confirm both clients use Tussenruimte, their network avatars spawn and remain near the corrected lobby, and the directory synchronizes room membership and questions. Verify disconnection frees seats and clears the browser/connection state. Do not redesign avatar or voice systems; fix only demonstrated integration faults necessary for this request. Dependencies: Steps 3–5.
7. **Test and build.** Run Edit Mode and multi-frame Play Mode checks, visually inspect the runtime browser and room overview, then build a new Quest APK at `build/Tussenruimte-Quest3-RoomBrowser.apk` (use a timestamp if that filename already exists). Keep previous APKs. Dependencies: Steps 2–6.

## New implementation phase: character styles and names
The room-browser phase above is complete. Continue with the following extension rather than reimplementing completed networking/UI work.

8. **Customization preflight.** Read current profile initialization, offline avatar visuals, head/hand tracking and network-avatar registration, name-tag/player-list rendering, and assembly boundaries. Perform dependency/impact analysis for the avatar prefab, all modified scripts, and Tussenruimte scene. Confirm the current input backend and preserve the startup scene, internet permission, and corrected spawn. Dependencies: completed Steps 1–7.
9. **Review three human designs.** Prepare matching concept/mockup previews for the three cozy human presets with clearly different hairstyles and upper-body clothing. Obtain visual approval before final authoring of new character geometry. Reuse the existing tracked head/hands source where suitable; explain any need for replacement assets rather than silently introducing full-body humanoids. Preserve source assets. Dependencies: Step 8; explicit visual approval is a gate for Step 11.
10. **Current-run player profile and name entry.** Implement CozyPlayerProfile with a default display name, selected preset, safe scalar/UTF-8 name validation, draft/apply/cancel handling, and reset on app launch. Integrate with template LocalPlayerName/LocalPlayerColor and coordinate OfflineMenu initialization. Keep choices through same-run disconnect/reconnect but never save them to PlayerPrefs, disk, or cloud. Make relevant name displays literal text. Dependencies: Step 8.
11. **Author and wire the approved presets.** Create the approved lightweight hairstyles/clothing visual variations and catalog under Generated/Avatars. Make a Tussenruimte-specific avatar variant using the existing tracking transforms and hands. Preserve mouth movement where compatible, local head hiding, material ownership, and correct shadow/mirror behavior. Configure preview/offline counterparts. Do not add full-body IK or locomotion/animation setup. Dependencies: Step 9 approval and Step 10.
12. **Replicate preset choice.** Add owner-written preset ID synchronization using the existing NGO/DA player ownership. Validate preset IDs against the catalog, initialize from this run's profile on spawn, and apply the synchronized choice to remote avatars and late joiners. Keep the existing networked name field. Update Tussenruimte player-prefab registration and compatibility metadata/version so all participants use the new matching build. Avoid assembly reference cycles and do not create another NetworkManager. Dependencies: Steps 8, 10–11.
13. **Customization menu and preview.** Add Mijn karakter to the existing welcome flow, three selectable preset cards, current name, Naam wijzigen, Toepassen, and Terug. Reuse CozyUiFactory and the VR keyboard. Show an isolated RenderTexture avatar preview with XR/stereo disabled on the preview camera; never replace or override the tracked gameplay camera. Support offline and connected edits, and stop preview rendering/unsubscribe when closed or disabled. Dependencies: Steps 10–12.
14. **Regression tests and Quest build.** Test profile validation/lifecycle, three-preset selection and fallback, draft cancellation, local/remote synchronization, late joiners, reconnects, head hiding, and preview camera isolation. Re-run room/browser and spawn regressions. Build `build/Tussenruimte-Quest3-CharacterCustomization.apk` (timestamp if already present) with the same application ID and signing. Provide same-build two-headset test steps and clearly state any unverified visual or multiplayer behavior. Dependencies: Steps 10–13.

# Verification & Testing
- Customization unit checks: whitespace/empty/control input, combining characters and supplementary Unicode, UTF-8 payload capacity, overlong names, literal markup, duplicate names, unknown/out-of-range preset IDs, apply vs cancel, and no profile persistence APIs.
- Name/appearance runtime checks: selected name and human style survive same-run leaving/rejoining, but reset after a fresh app launch. Template defaults must not overwrite an applied selection.
- Preview/UI checks: exactly one tracked gameplay camera still targets the headset; the preview camera renders only its RenderTexture and has XR/stereo disabled. Preset cards and names are readable, panels do not overlap, and preview rendering stops when closed.
- Network acceptance: two clients install the same customization build; both observe each other's name and preset. Change a preset/name while connected, then test a late joiner and reconnection. Invalid selections cannot select arbitrary objects or affect another player's avatar.
- Character visual checks: three distinct human looks, correct head/hand alignment, no first-person head clipping, matching offline preview, readable remote name tags, and maintained Quest frame performance. No full-body skeleton or locomotion system is introduced.
- Unit tests: compatibility filtering, full/locked/incorrect-scene games, missing metadata, count calculation, empty/error/loading transitions, duplicate request suppression, late results after disable, and pagination boundaries.
- Room overview: six entries, 0/2 and 1/2 joinable, 2/2 disabled; current room marked; rejected/full entry uses existing directory feedback. Offline state labelled as a preview.
- Runtime lifecycle: authentication before discovery/create/join; retry after failure; unsubscribe and stop refresh on disable/exit; no unintended second network start.
- XR regression: five-second spawn stability at the Tussenruimte lobby, no teleport-back loop, tracking driver remains enabled, controls still support head movement while seated.
- Visual checks: readable labels and counts on Quest, comfortably selectable buttons, no overlapping welcome/browser panels, loading/errors visible, long game names contained.
- Android build: succeeds; APK contains `com.cozyrooms.tussenruimte`, INTERNET permission, and only Tussenruimte as the startup game scene. Preserve signing so it can update the existing separate app.
- Required real-network acceptance test: install the same compatible new build on two Quest devices (or Quest plus a properly configured second client). A creates a public game; B sees it and joins. Repeat using the invitation code. Both see each other and identical room occupancy; one enters, the other sees 1/2; both enter and see 2/2; a third cannot enter; leaving/disconnecting frees a seat.
- Confirm both devices use the same cloud project/environment and distinct authenticated player identities. Test on separate home networks where possible.
- Clearly distinguish locally verified code/build results from cloud queries and two-device headset results. If a second device or cloud access is unavailable, deliver the APK and explicit remaining test steps; do not claim online functionality has been proven.
