# Requirements fulfillment audit

Audited 11 September 2026 against every user prompt in this project conversation, the current working tree, documentation, and the last recorded build/test run.

## Status key

- **Verified**: implemented and supported by recorded build/test or direct local evidence.
- **Partial**: code or documentation exists, but part of the requested behavior is missing or the required acceptance check was not performed.
- **Open**: not implemented.
- **Approval-gated**: correctly waiting for the user to review and authorize an external/public action.

## Result

The project has a sound rebuilt foundation, but the request as a whole is **not fulfilled**. Earlier reports treated code changes as finished too early. There is no evidence that the repeatedly reported artwork, mapping, theme, and layout problems have been tested successfully with real audio files and the requested UI states.

| User request cluster | Assessment | Evidence | Missing or incorrect work |
| --- | --- | --- | --- |
| Analyze the original decade-old project: what was right/wrong and how to improve. | **Verified** | `docs/CODE_REVIEW.md` gives a legacy-code assessment with concrete source references. | None for the requested report. |
| Recommend a current Windows stack; explain solution format vs UWP; plan a ground-up compatible rewrite. | **Verified** | `docs/MODERNIZATION_PLAN.md`; SDK-style .NET 10 WPF solution exists. | Windows 10/ARM64 compatibility was not validated; Windows 11 x64 is the only documented local target. |
| Start by acquiring SDK and execute the plan. | **Verified, partial scope** | `global.json` pins 10.0.401; Release build has passed. | The multi-phase plan itself is still incomplete. |
| Use a current tag library and assess extension support beyond MP3/M4A. | **Partial** | `z440.atl.core` 7.16.0 is used; picker lists MP3, M4A/AAC, FLAC, Ogg/Oga, Opus, WAV, AIFF/AIF, WMA, M4B. | No representative round-trip/per-format preservation tests. Extension list must not be presented as proven support. |
| Confirm iTunes API availability and identify returned fields that are real audio metadata. | **Partial** | Live JSON evidence is in `docs/evidence/itunes-probe-2026-09-11.json`; README provides a high-level distinction. | No complete, field-by-field user-facing audit that maps every returned field to write/no-write/rationale. |
| Write all legitimate iTunes metadata and make tags viewable/editable. | **Partial** | Basic fields and Full tags are implemented in code. | Explicitness, artwork during batch tagging, and several catalog values are not mapped; Full tags needs real MP3/M4A verification and a clear unsupported-field experience. |
| Make local tags resemble an iTunes-purchased file as far as legitimate song data allows. | **Partial** | README correctly excludes ownership/purchase/DRM data. | Batch artwork is not downloaded/embedded; metadata coverage is incomplete and not verified against a purchased-file reference. |
| Remove BPM if Apple does not supply it; replace Comment with correctly labelled unsynchronized lyrics. | **Implemented candidate** | Main editor exposes `Unsynchronized lyrics`; no BPM or Comment control appears in `MainWindow.xaml`. | Requires UI and write/read-back verification. |
| Explain/remove Website and ISRC from the normal form. | **Partial** | They are absent from the main editor and documented in README; retained in Full tags. | Full-tags real-file round trip has not been demonstrated. |
| When Apple lacks a value, clear retained website URLs and preserve other text. | **Partial** | `WebsiteUrlCleaner` unit tests exist. | It is not integrated into the album tagging/write-plan workflow; no preview, per-field explanation, or real-file integration test exists. |
| Search albums precisely, like the original query; do not show songs/podcasts. | **Partial** | Provider uses `media=music`, `entity=album`, `limit=70`, album collection filtering. | The UI currently calls `includeExplicit: true`, whereas the legacy query used `explicit=no`; Lorde/Pure Heroine and other exact searches have not been revalidated after the rewrite. |
| Do not dilute album results with broad artist/song lookups. | **Implemented candidate** | Broad artist merge was removed; only exact `collectionName` song fallback remains. | Needs live regression tests and result inspection. |
| Select several local files, choose an album, match/reorder tracks, and write all at once. | **Partial** | Album mapping dialog and batch writer exist. | No real five-file test, no robust per-file failure/cancellation handling, no staging preview, no saved/skipped/failed report, no artwork/URL cleanup in batch path. |
| Mapping dialog: numbered left tracks, numbered right files, equal row counts, center arrow controls, no silent drops. | **Implemented candidate** | `AlbumMappingWindow` uses padded slot collections instead of `Zip` and shows center arrows. | It has not been manually tested; unmatched files/tracks are not summarized before saving. |
| Imported file checkbox column; clicking row/checkbox highlights it; header selects/clears all. | **Implemented candidate** | Checkbox binding, header binding, and `IsSelectedForAlbum` row style exist. | Must be tested for mouse, keyboard, multi-select, and header-state behavior. |
| Keep Load tracks available in a small result table. | **Implemented candidate** | Load column is first/frozen in `MainWindow.xaml`. | Needs small-window visual verification. |
| Status bar must show `X of Y selected | Z staged | N saved`. | **Implemented candidate** | `SelectionSummary` constructs that text. | Needs interaction test; it has not been verified for all state transitions. |
| Dark/light/System theme, readable input colors/table headers, compact Theme picker, selected rows, and accessible UI. | **Partial** | Theme resources and row/header styling exist. | Repeated user reports show this has not reached acceptance; no documented System/Light/Dark visual test. |
| Correct Find an album height and vertical text alignment. | **Implemented candidate** | Search `TextBox` is explicitly 30px and vertically centered. | Needs visual confirmation at runtime. |
| Put editor heading/subtitle above artwork; keep right panel padded and usable. | **Implemented candidate** | XAML order and scroll/padding are present. | User previously reported the opposite behavior; no visual acceptance proof. |
| Square artwork selector, clear placeholder, show selected file cover, open image picker, write/verify/reload it. | **Partial** | Front-cover service, image decoding, placeholder binding, picker command, and 176x176 control exist. | No real MP3/M4A verification. This is the most repeatedly reported functional failure and must be tested before it is called fixed. |
| Use 600x600 artwork and explain purchased-file artwork quality. | **Partial** | Provider rewrites artwork URL to 600x600; caption is shown. | Artwork is not fetched/embedded during album batch writes; no comparison with iTunes-purchased files or per-format artwork test. |
| Add matching action icons from an open-source library. | **Implemented candidate** | FluentIcons.Wpf is referenced and used in the UI. | Requires accessibility/visual review. |
| Generate a distinguishable custom app icon, correct taskbar size/contrast, document it. | **Partial** | ICO asset is configured and README includes the icon. | Taskbar appearance was reported unsatisfactory and has no user acceptance verification. |
| Add Full tags with Tag name/Value table, Add, selected-row Edit, editable cells, and Save. | **Implemented candidate** | `FullTagsWindow` and `ApplyFullTagsAsync` implement this workflow. | Not tested against actual files; no delete/clear action; format-specific custom-tag support is not surfaced before save. |
| Run the rebuilt program locally for inspection. | **Verified** | Local `ThirdAlien.App` process was launched; current process is running. | A launch/smoke check is not feature acceptance. |
| Add rich README: purpose, workings, features, libraries/versions, compatibility and useful details. | **Partial** | README was rewritten with those sections. | It has no real workflow screenshot/GIF yet; some claims remain contingent on required real-file verification. |
| Prepare installable and portable executable for GitHub Releases. | **Open, approval-gated for publication** | Modernization plan documents intended release strategy. | No self-contained portable artifact, installer, checksums, release notes, or release has been built. |
| Screen-record actual workflow, convert to GIF, add to README, upload/push. | **Open, approval-gated for publication** | README states it is remaining work. | No recording, GIF, README asset, commit, push, or release. |
| Push to GitHub after user says the app is good to go. | **Correctly not performed** | Working tree remains uncommitted/unpublished. | Wait for user acceptance after all requested behavior is verified. |
| Use GPT-6 for analysis, then GPT-5.6 Terra High for execution. | **Not verifiable / not fulfilled as a workflow guarantee** | No project artifact can prove model selection. | The active environment must expose a model-selection control; do not state that this was completed otherwise. |
| Add durable memory so the whole list is not forgotten. | **Verified** | `AGENTS.md` now contains the consolidated scope and active acceptance checklist. | This audit is linked from that memory below. |

## Explicitly open requirements that were previously treated as done too early

1. Real MP3 and M4A artwork display/pick/write/reopen verification.
2. Real MP3 and M4A Full tags add/edit/save/reopen verification.
3. Five-file album matching regression test and unmatched-row reporting.
4. System/Light/Dark visual acceptance test, including headers, inputs, selection, theme picker, and narrow layout.
5. Strict iTunes result regression test, especially Lorde - *Pure Heroine*.
6. Full iTunes response-field mapping report.
7. Album artwork download/embed in the batch tag path.
8. URL-cleaning integration with a reviewable batch write plan.
9. Per-format tag support/preservation tests.
10. Portable build, installer, release preparation, demo recording/GIF, then user-approved publication.

## Verification record

The latest recorded code verification is `dotnet build ThirdAlien.slnx -c Release` and `dotnet test ThirdAlien.slnx -c Release --no-build`, with 11 passing tests. Those tests cover URL-cleaner logic, matching rules, and safe temporary-copy commits; they do **not** prove the unverified UI and real-audio behavior above.