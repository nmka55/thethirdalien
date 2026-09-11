# Current implementation conformance review

Reviewed 11 September 2026 against `AGENTS.md`, `docs/REQUIREMENTS_AUDIT.md`, the WPF source, catalog/tag services, and tests. This is a source review. It does not substitute for the required real MP3/M4A and visual acceptance tests.

## Resolution update - 11 September 2026

The following findings were repaired after this review:

- **C1**: selection and artwork-save paths now update `HasFrontCover`; M4A `Generic` covers are recognised alongside `Front` covers.
- **C2**: corrupted save/search status strings were replaced with clean text.
- **C3**: batch writes now catch per-file failures, retain safe individual commits, report saved/failed/unmatched/cleaned counts, and require confirmation before partial mappings.
- **H1**: batch writes now obtain the selected album artwork once, combine it with the metadata patch in one verified commit, and apply conservative URL cleanup to retained Composer, Lyrics, and Website values.
- **H2**: System theme now derives its dark/light value from Windows and uses system colors in high contrast.
- **H3**: the UI default now uses `explicit=no`; the extra song request runs only when no exact album name was returned.
- **H6**: artwork and Full tags saves now set the file state to `Saved`, so the status summary counts them.

See [verification evidence](evidence/verification-2026-09-11.md). The remaining visual and five-file acceptance checks remain open.
## Overall conclusion

The rebuild has the right structure, but it does not yet meet the user-facing acceptance requirements. Several reported UI defects are directly explained by current source code. Do not describe the app as ready for user review, packaging, or release until the critical findings are fixed and the open verification checklist is completed.

## Critical findings

| ID | Finding | Requirement impact | Evidence |
| --- | --- | --- | --- |
| C1 | The cover placeholder is always shown for a selected file and after selecting new cover art. `HasFrontCover` is declared but is only updated after saving through Full tags; it is not updated after a file selection or artwork write. The placeholder's visibility depends on that value. | Directly causes the reported "artwork not showing" and makes it look as though the picker did nothing. | [MainViewModel.cs](../src/ThirdAlien.App/MainViewModel.cs#L45), [MainViewModel.cs](../src/ThirdAlien.App/MainViewModel.cs#L62), [MainViewModel.cs](../src/ThirdAlien.App/MainViewModel.cs#L79), [MainViewModel.cs](../src/ThirdAlien.App/MainViewModel.cs#L132), [MainWindow.xaml](../src/ThirdAlien.App/MainWindow.xaml#L62) |
| C2 | Individual Save and Search status messages contain massive mojibake text, not a normal ellipsis/loading message. | Breaks the requested clean, accessible UI and confirms the user-facing text corruption complaint. | [MainViewModel.cs](../src/ThirdAlien.App/MainViewModel.cs#L201), [MainViewModel.cs](../src/ThirdAlien.App/MainViewModel.cs#L231) |
| C3 | Album batch writes have no per-file failure handling. An `IOException`, validation error, unsupported format, or authorization failure inside `WriteAlbumMappingsAsync` escapes the method; the caller only catches catalog/network exceptions. The loop stops and no saved/skipped/failed summary is produced. | Violates safe, reviewable batch tagging and can leave a partially written album without a clear result. Each individual file still uses the backup path, but the batch experience is not safe enough. | [MainViewModel.cs](../src/ThirdAlien.App/MainViewModel.cs#L256), [MainViewModel.cs](../src/ThirdAlien.App/MainViewModel.cs#L269) |

## High-priority findings

| ID | Finding | Requirement impact | Evidence |
| --- | --- | --- | --- |
| H1 | The batch tag path writes only title, artist, album, album artist, numbers, genre, copyright, and year. It does not download/embed the selected album artwork, run the approved URL cleaner, or report unmatched rows. | The requested "tag the entire album" workflow and iTunes-like metadata result are incomplete. | [MainViewModel.cs](../src/ThirdAlien.App/MainViewModel.cs#L269) |
| H2 | `System` theme is implemented as Light. `ApplyTheme` checks only whether the value is `Dark`; every other option uses light colors. It does not follow Windows preference, high contrast, or theme changes. | Does not fulfill the System/Light/Dark requirement. | [MainViewModel.cs](../src/ThirdAlien.App/MainViewModel.cs#L316) |
| H3 | The current search is stricter than the earlier broad regression, but the UI explicitly passes `includeExplicit: true` while the legacy query used `explicit=no`. It also always issues an additional 200-song exact-title request, even when the strict album result is sufficient. | The result behavior still differs from the original request and has not been validated with the reported Lorde - *Pure Heroine* case. | [MainViewModel.cs](../src/ThirdAlien.App/MainViewModel.cs#L232), [ITunesCatalogProvider.cs](../src/ThirdAlien.Infrastructure/ITunesCatalogProvider.cs#L27), [ITunesCatalogProvider.cs](../src/ThirdAlien.Infrastructure/ITunesCatalogProvider.cs#L47) |
| H4 | Full tags can add/edit text fields, but it has no explicit clear/delete operation. It also relies only on ATL's standard properties and `AdditionalFields`; unsupported/opaque records are not shown read-only with an explanation. | The requested advanced metadata editor is incomplete and cannot yet justify an "all tags" claim. | [FullTagsWindow.xaml](../src/ThirdAlien.App/FullTagsWindow.xaml#L9), [FullTagsWindow.xaml.cs](../src/ThirdAlien.App/FullTagsWindow.xaml.cs#L39), [AtlAudioTagService.cs](../src/ThirdAlien.Infrastructure/AtlAudioTagService.cs#L107) |
| H5 | Artwork handling exists for user-picked art, but no tests cover read/display/pick/write/reopen on MP3 or M4A. | The repeatedly reported artwork failure remains unverified. | [AtlAudioTagService.cs](../src/ThirdAlien.Infrastructure/AtlAudioTagService.cs#L25); test project contains no ATL artwork tests. |
| H6 | The status summary counts files only when their state is `Saved` or `Album tags saved`. Immediate Full tags and artwork saves do not change file state, so the requested saved count will be wrong after those operations. | Status bar does not truthfully represent all saves. | [MainViewModel.cs](../src/ThirdAlien.App/MainViewModel.cs#L51), [MainViewModel.cs](../src/ThirdAlien.App/MainViewModel.cs#L77), [MainViewModel.cs](../src/ThirdAlien.App/MainViewModel.cs#L128) |

## Medium-priority findings

| ID | Finding | Requirement impact | Evidence |
| --- | --- | --- | --- |
| M1 | `Website` remains as an unused ViewModel property even though it was removed from the main form. | The normal UI is cleaner, but the code/model cleanup is incomplete. | [MainViewModel.cs](../src/ThirdAlien.App/MainViewModel.cs#L44) |
| M2 | The mapping dialog preserves row count by padding the shorter list and correctly filters empty pairs for save. However it does not display a separate unmatched-track/unmatched-file count or ask for confirmation before saving partial mappings. | Prevents silent `Zip` truncation but does not yet provide the required explicit unmatched review. | [AlbumMappingWindow.xaml.cs](../src/ThirdAlien.App/AlbumMappingWindow.xaml.cs#L17), [AlbumMappingWindow.xaml.cs](../src/ThirdAlien.App/AlbumMappingWindow.xaml.cs#L28) |
| M3 | Moving a file sets `SelectedSlot` without property-change notification. The swap updates row values, but the grid selection may not visibly follow the moved file. | Weakens the requested clear reorder experience. | [AlbumMappingWindow.xaml.cs](../src/ThirdAlien.App/AlbumMappingWindow.xaml.cs#L12), [AlbumMappingWindow.xaml.cs](../src/ThirdAlien.App/AlbumMappingWindow.xaml.cs#L53) |
| M4 | The file-table selection handler only marks newly selected rows checked; it does not toggle the check state when a row is selected again. | It meets "select row to check" but not an intuitive click-to-toggle interpretation. Header clear/select behavior still exists. | [MainWindow.xaml.cs](../src/ThirdAlien.App/MainWindow.xaml.cs#L10) |
| M5 | Row foreground is set at the `DataGridCell` level, so it can override the selected-row foreground assigned at the row level. | Selected rows may still have low contrast in some theme combinations. | [App.xaml](../src/ThirdAlien.App/App.xaml#L56), [App.xaml](../src/ThirdAlien.App/App.xaml#L70) |
| M6 | The app uses a 600x600 URL rewrite, but the code does not fetch that URL in the batch path or verify actual source dimensions/content type. | Caption and URL intent exist; "same size as iTunes artwork" is not delivered. | [ITunesCatalogProvider.cs](../src/ThirdAlien.Infrastructure/ITunesCatalogProvider.cs#L156), [MainWindow.xaml](../src/ThirdAlien.App/MainWindow.xaml#L69) |
| M7 | Search does not expose storefront or explicit-content choice, has no cancellation command, and has no tests for provider result filtering/ordering. | Makes catalog behavior less controllable and leaves the prior precision regression insufficiently guarded. | [MainViewModel.cs](../src/ThirdAlien.App/MainViewModel.cs#L232), [ITunesCatalogProvider.cs](../src/ThirdAlien.Infrastructure/ITunesCatalogProvider.cs#L14) |

## Requirements that current code does satisfy structurally

- The solution uses SDK-style .NET 10 projects, WPF, a separable `ICatalogProvider`, and an `IAudioTagService`.
- File writes use a temporary same-directory copy, reopen verification, replacement, and a backup path.
- The primary catalog query is restricted to `media=music` and `entity=album`, with collection-album filtering. It no longer merges broad artist results.
- The main form has a right-side editor, compact 30px search field, checkbox column/header checkbox, status bar, Full tags button, album mapping arrows between tables, and a frozen Load action column.
- The normal form uses Unsynchronized lyrics and no normal Website/ISRC controls.
- The custom icon and Fluent icon package are configured.

These are implementation facts, not user-acceptance proof.

## Test coverage gap

The recorded 11 tests cover `WebsiteUrlCleaner`, `TrackMatcher`, and `SafeFileCommitService`. There are no automated tests for:

- iTunes provider request/result filtering and the exact-title fallback;
- `AtlAudioTagService` reading/writing regular tags, additional tags, or front-cover art;
- ViewModel state transitions and status counts;
- five-file mapping behavior;
- WPF theme, selection, or layout behavior.

## Recommended repair order

1. Fix C1, C2, and C3 before further UI polishing.
2. Add disposable MP3 and M4A integration fixtures for ordinary tags, Full tags, and artwork read/write/reopen verification.
3. Make album batch tagging build an explicit plan: matched files, unmatched files/tracks, catalog fields, artwork download, URL-cleanup proposals, and per-file saved/skipped/failed outcomes.
4. Restore the intended catalog policy deliberately: choose whether explicit content defaults off as in the legacy app, make it a visible setting if needed, and add provider tests for `Pure Heroine` and non-music exclusion.
5. Implement a real System theme/high-contrast path and perform the required Light/Dark/System visual test matrix.
6. Only then prepare packaging, recording, GitHub release, and user-approved publishing.