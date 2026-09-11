# The Third Alien — rebuild plan

Prepared 11 September 2026, following the [code review](CODE_REVIEW.md). This is a proposed implementation specification, not a list of completed features.

**Recommended direction**

Build a local Windows desktop application in **C# on .NET 10 LTS, with WPF, MVVM, and a replaceable audio metadata service**. Keep the original idea: fast catalog-assisted editing of local audio. Add album matching, deliberate batch writes, a complete supported-field editor, and accessible System/Light/Dark themes.

.NET 10 is the stable LTS baseline, supported through November 2028; use its current servicing release and pin the installed SDK in `global.json` when implementation begins. Do not choose a preview runtime just for a newer version number. [Microsoft support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)

**Solution files, UWP, WPF, and WinUI are different choices**

`.sln` is a solution file that groups projects; it is not a UI framework. `.csproj` describes a C# project. Both remain usable with modern .NET, and the tooling also accepts the newer `.slnx` solution format. Use SDK-style projects and a single `ThirdAlien.slnx` for the rebuild; `.sln` is also fine if an editor needs it. [Microsoft build/publish tooling](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish)

Assuming “uwb” means **UWP**, do not start the rebuild with UWP. Microsoft describes it as in maintenance mode and recommends **WinUI 3 with Windows App SDK** for new native Windows applications. Existing WPF and Windows Forms apps are still supported development paths. [Microsoft's framework guidance](https://learn.microsoft.com/en-us/windows/apps/)

| Option | Fit for this app | Decision |
| --- | --- | --- |
| WPF on .NET 10 | Mature editable DataGrid, binding, desktop file access, keyboard workflows, and straightforward deployment | Recommended for this particular editor |
| WinUI 3 / Windows App SDK | Microsoft's preferred new-app platform, with native Fluent controls; requires proving the chosen editable-grid and deployment approach | Strong alternative if native Windows visual integration takes priority |
| Modern Windows Forms | Smallest incremental migration, but would keep more of the old form structure and require substantial theme/layout work | Reasonable for a minimal refresh; less useful for this ground-up redesign |
| UWP | Maintenance-oriented path | Do not select for the rebuild |

WPF is a project-specific recommendation, not a claim that it supersedes Microsoft's WinUI recommendation. .NET 10 includes improvements to WPF's Fluent styling, but Microsoft still describes that styling work as ongoing. Prototype the actual grid, editor, menus, dialogs, and high-contrast behavior early. [WPF .NET 10 changes](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/whats-new/net100)

**Proposed stack and structure**

| Concern | Choice |
| --- | --- |
| Desktop | C#, SDK-style `net10.0-windows` WPF project; nullable reference types enabled |
| Presentation | `CommunityToolkit.Mvvm`, data binding, commands, validation; minimal view code |
| Networking | Reused `HttpClient`, `System.Text.Json`, typed nullable response objects, cancellation |
| Metadata writer | Initial candidate ATL.NET `z440.atl.core` 7.16.0, selected only after the preservation spike below |
| Writer comparison | TagLibSharp 2.3.0 as a comparison/fallback candidate, not a second production write pass |
| Storage | JSON settings and a small local operation journal; bounded catalog cache; no server/database required |
| Tests | .NET test project for matching, parsing, merge rules, write recovery, and real format round trips |
| Delivery | Self-contained Windows x64 portable build and an Inno Setup installer; ARM64 after validation |
| Automation | GitHub Actions for restore/build/test/package, dependency updates, checksums, and release artifacts |

The MVVM Toolkit is UI-framework independent, allowing application behavior to be tested separately from WPF. [Microsoft MVVM Toolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)

Suggested projects: `src/ThirdAlien.App`, `src/ThirdAlien.Core`, `src/ThirdAlien.Infrastructure`, and `tests/ThirdAlien.Tests`. Core owns `AudioFileSnapshot`, `EditableMetadata`, `MetadataPatch`, `CatalogAlbum`, `CatalogTrack`, `TrackMatch`, `TagWritePlan`, and `WriteResult`. Infrastructure implements `ICatalogProvider`, `IAudioTagService`, and `IFileCommitService`. A field capability record describes read/write support, native mapping, and validation for each format.

The core must not reference windows, grid cells, or image controls. The application owns selected files and pending edits; services open and close files for individual operations. Preserve a clean seam for another catalog provider without building that provider now.

**1. iTunes availability and metadata scope**

The public iTunes Search API still worked in our live check. The existing `search` and `lookup` approach is usable today without an API key. This is the iTunes Store catalog, not guaranteed coverage of the entire Apple Music catalog or a user's personal library. Apple's documentation is archived; the successful check demonstrates present operation, not a future service guarantee. [Apple overview](https://developer.apple.com/library/archive/documentation/AudioVideo/Conceptual/iTuneSearchAPI/index.html), [local response evidence](evidence/itunes-probe-2026-09-11.json)

Use album search followed by an ID lookup with `entity=song`, matching the selected storefront. Separate the collection record from song records. The existing app already implements the rough shape of these two requests. [Apple lookup examples](https://developer.apple.com/library/archive/documentation/AudioVideo/Conceptual/iTuneSearchAPI/LookupExamples.html)

Use properly encoded queries, a configurable storefront, and an explicit-content option that includes all editions by default. Apple documents a maximum result limit of 200 and approximately 20 calls per minute, subject to change. Start with a conservative request budget below that rate, debounce searches, cache repeat lookups, respect throttling responses, and handle timeout/offline/malformed data separately. A limit is not a guarantee of completeness; flag partial albums instead of claiming all songs were found. [Apple request parameters and limits](https://developer.apple.com/library/archive/documentation/AudioVideo/Conceptual/iTuneSearchAPI/Searching.html)

The following mapping is based on the live response and the documented result structure. Fields may be missing or differ by record type, edition, and storefront. [Apple result structure](https://developer.apple.com/library/archive/documentation/AudioVideo/Conceptual/iTuneSearchAPI/UnderstandingSearchResults.html)

| Apple value | Proposed app behavior |
| --- | --- |
| Song `trackName` | Editable title |
| Song `artistName` | Editable track artist; retain the display string intact |
| `collectionName` | Editable album |
| Collection `artistName` | Proposed album artist; distinguish from song artist and allow correction |
| `trackNumber`, `trackCount` | Editable track number/count; validate scope for multi-disc releases |
| `discNumber`, `discCount` | Editable disc number/count |
| `releaseDate` | Preserve complete source dates; default album workflow to collection date; derive year when the format only supports year |
| `primaryGenreName` | Editable genre |
| Album `copyright` | Editable copyright where the format supports it; not automatically a record-label value |
| Track/collection explicitness | Content advisory where the native tag supports it; otherwise source detail or an explicitly selected custom field |
| `artworkUrl30/60/100` | Cover candidate; embedding is subject to artwork-use review below |
| `trackId`, `collectionId`, `artistId` | Source identifiers; optional native/custom-tag mappings with clear labels |
| Store/artist URLs | View/open source information; optional supported URL tags |
| `trackTimeMillis` | Matching evidence and source display; local duration remains read-only and independently measured |
| `country`, `currency`, prices, `isStreamable`, censored names, `previewUrl`, response type fields | Viewable catalog details; not automatically written as music tags |

The app will offer editable composer, lyricist, lyrics text, BPM, key, comment, grouping, conductor, publisher/label, original date, sort fields, compilation status, ISRC, and supported custom fields even when the Search API cannot populate them. Do not promise those values from the Search API or invent them. Existing ReplayGain, MusicBrainz IDs, chapters, extra pictures, and other advanced metadata must be preserved unless explicitly edited. Gains and peaks should be edited only through validated fields, not confused with volume conversion.

“All tags” means **all fields our selected backend can safely read and write for that file format**. Provide Basic, Details, Artwork, and Advanced views. Show supported custom text fields with add/edit/remove actions and validation. Show opaque or unsupported records read-only with an explanation; preserve them during unrelated edits. Technical audio properties are always read-only. Do not serialize the whole API response into every file by default.

Missing source values normally mean **keep existing**, with the user-requested website-URL cleanup exception below. Every field needs Keep / Set / Clear semantics so a blank textbox or mixed multi-selection cannot accidentally erase data. Offer fill-missing and replace-selected-fields modes; show which values came from the file, Apple, or the user. Bulk fields with differing values display “Multiple values” until the user explicitly changes them.

**Website-URL cleanup for retained tags — user requirement added 11 September 2026**

When iTunes supplies no usable replacement for a field, inspect its existing textual values before retaining them. Automatically propose removing website URLs, including in comments, lyrics, website fields, and supported custom text tags. This cleanup is enabled by default for the iTunes tagging workflow and uses the same preview, write verification, backup, and restore path as other edits.

- If a value consists only of a website address, clear that value. Recognize HTTP/HTTPS URLs, `www.` addresses, and confidently recognizable bare domains, including optional paths/query strings. Match scheme and host case-insensitively; account for surrounding whitespace, brackets, and internationalized domains.
- If a value mixes meaningful text with a website URL, remove the URL and retain the other text. For multi-value fields, inspect each element independently and retain non-URL elements; clear the field only if no meaningful values remain. This is the default interpretation of erasing website URLs without losing unrelated metadata.
- Use a URL parser and conservative domain recognition rather than treating every dotted string as a website. Do not visit detected addresses or require network access. Preserve email addresses, local file paths, filenames, dates, identifiers, and ambiguous dotted text that is not confidently a website address.
- Inspect supported structured website-tag values as well as ordinary text tags. Never apply text replacement to artwork, audio, binary/opaque tags, or container internals. If a tag cannot be safely decoded or rewritten, show that limitation rather than silently claiming it was cleaned.
- Apply the rule only to pre-existing values retained because iTunes has no usable value for that field. It does not automatically remove newly fetched Apple source links, fields deliberately excluded from the operation, or values the user explicitly enters in the current edit. An explicitly supplied user edit takes precedence over the automatic cleanup proposal.
- In the preview, identify each affected file, field, original value, and proposed result with the reason **Website URL removed**. URL-only deletions must become explicit Clear operations in the write plan, not empty values that the normal preserve-missing rule ignores. Saving commits approved preview changes; fetching metadata alone never modifies files.

Acceptance cases: URL-only comments; bare-domain and `www.` values; HTTPS addresses with paths/query strings; mixed lyrics or comments with an embedded link; multi-value custom tags containing one URL; structured website tags; Unicode domains; and unchanged non-URL metadata. Verify false-positive preservation for email addresses, filenames, dates, and ordinary dotted text; also test preview exclusion, explicit user-edit precedence, save/read-back, and backup restoration.

Apple Music API is a separate optional future provider. Its song attributes can include composer and ISRC; `hasLyrics` is an availability flag, not lyrics text. It requires developer-token authorization and introduces credential management. Do not bundle a private signing key in a desktop executable. It is unnecessary for the first rebuilt version. [Apple Music song attributes](https://developer.apple.com/documentation/applemusicapi/songs/attributes-data.dictionary), [developer tokens](https://developer.apple.com/documentation/applemusicapi/generating-developer-tokens)

Artwork deserves a release-specific check: Apple's archived terms impose promotional-use conditions on artwork and previews. Successful downloading does not establish permission for unrestricted permanent embedding or redistribution. Review the intended artwork feature against applicable terms before public release; meanwhile the editor can fully support existing covers and user-supplied images. Do not add preview downloading. [Apple promotional-content conditions](https://developer.apple.com/library/archive/documentation/AudioVideo/Conceptual/iTuneSearchAPI/index.html)

**2. Additional audio formats**

Yes. The old app's MP3/M4A restriction is explicitly in its file picker, not an iTunes restriction. Metadata matching is independent of the audio codec. Both candidate libraries cover more formats, but a library's format list is not a promise that every field works identically. ATL publishes a read/write matrix; use it as the starting point, then verify our own fixtures. [ATL format support](https://github.com/Zeugma440/atldotnet)

| Planned support | Files | Required validation |
| --- | --- | --- |
| First release core | MP3; M4A containing AAC or ALAC; FLAC; Ogg Vorbis; Opus | Core tags, Unicode, numbering, artwork, custom fields, preserved audio and unrelated tags |
| First release, after dedicated checks | WAV; AIFF/AIF; WMA/ASF; M4B | Native tagging behavior and player interoperability; preserve chapters in M4B |
| Follow-on coverage | APE, WavPack/WV, MPC, DSF and other useful library-supported types | Real fixtures and explicit capability matrix before enabling writes |
| Restricted or unsupported | DRM-protected media; raw AAC or unusual containers without validated tag conventions | Explain the limitation; do not imply decryption or universal tag interoperability |

Do not transcode audio to tag it. Detect actual container/content where supported; do not trust only an extension. Reject malformed files individually. Keep tag dialects compatible with existing files where possible, and document the selected defaults for new tags. Use Windows case-insensitive path deduplication, stable file identity checks, and explicit handling of links/hard links so a batch cannot unknowingly write the same file twice.

**3. Album matching and one-action batch writing**

The workflow should be:

1. Add files or a folder, select the files belonging to an album, and choose **Find album**.
2. Seed search from existing album/artist tags; let the user edit the query and storefront. Show cover, artist, album, date, track count, and explicitness for each edition.
3. Load the chosen album once and propose file-to-track matches. Show disc/track numbers and duration on both sides.
4. Review a mapping table with file, proposed song, confidence/reason, and status. Support selecting another song, swapping matches, excluding a file, or leaving it unmatched using keyboard-accessible controls.
5. Review per-file and shared-field diffs; edit any proposed metadata. Choose which fields to apply and what to preserve.
6. Choose **Save selected changes** once. Show progress, allow cancellation between safe file operations, and report saved, skipped, failed, and cancelled items individually.

Matching must be a one-to-one assignment across the selected group, not a filename sort zipped against API order. Compute candidate scores using existing title, cautiously normalized filename, disc/track number, artist, and duration. Preserve version markers such as live, remix, instrumental, clean, and remaster; normalization must not erase differences that identify another recording.

Lock user-confirmed assignments, score remaining candidates, and find the best assignment while allowing files to remain unmatched. Use both score and the gap from alternative matches to identify ambiguity. Treat confidence as a heuristic, not a calibrated probability. Choose weights and thresholds from fixtures instead of claiming arbitrary numeric thresholds are proven.

Missing tags can make duration/filename matching useful, but neither proves identity. Duplicate titles, partial albums, bonus tracks, multi-disc releases, compilation artists, count mismatches, and alternate editions require explicit tests. An ambiguous match should need review before inclusion in a write plan. Never fetch one album per local track or silently reuse one song for multiple files. Acoustic fingerprinting is separate future work.

**Write-safety contract**

- Import creates an immutable before-snapshot and records file identity, length, and modification time. Changes remain staged. At commit, revalidate identity/content state and refuse stale plans when another application has modified the file.
- Preflight the selected batch: readable source, writable destination, sufficient space for copies/backups, valid metadata, valid mappings. Use bounded background reads; serialize commits initially.
- Create a uniquely named temporary copy in the same directory/volume, retaining a recognizable file extension or an explicit backend format hint. Apply only the selected patch to the copy. Dispose all library handles, flush, reopen, and verify expected tags, preserved fields, readable media, and unchanged audio properties.
- Commit using a filesystem-supported replacement that retains the original as a unique backup. Never silently fall back to overwriting the original in place. Where reliable replacement cannot be established, report the file as unsupported for safe commit until a tested recovery path exists.
- Keep a journal with recoverable prepared/committed/failed state. Detect incomplete operations on restart, retain backups until deliberate cleanup, and avoid logging more personal metadata than necessary. If staging disk space is unavailable, do not start the write.
- “Batch save” is one user action with per-file commits, **not an atomic transaction across the whole album**. On failure, leave untouched files intact and report which were already committed. Cancellation must not interrupt a file replacement halfway through.
- Provide undo/restore from backups. Check that the current file still matches our committed result before restoring, so undo does not overwrite later edits made by another app.

Only extend the writer's advertised capability after tests demonstrate preservation. Include artwork bytes/types, multiple artists, hidden custom fields, chapters, tag-family conflicts, and exact audio-payload preservation where measurable. Round-trip equality via the same library alone is insufficient: check representative files using an independent reader and decoded audio comparison. Synthetic audio generated with FFmpeg is suitable for tests; FFmpeg need not ship as a runtime dependency.

**4–5. Theme, layout, and accessibility**

Use a resizable workspace with a top command bar, a multi-select file table, a right-hand editor, and a lower status area. Search/matching can occupy a panel or a focused review screen without the old chain of modal forms. The file table shows filename, title, artist, album, disc/track, duration, format, and pending/error status. The editor supports per-file and common selection edits; advanced fields remain discoverable without crowding the basic form.

Theme choices: **System** by default, **Light**, and **Dark**, persisted and changeable while running. Base colors, focus indicators, selection states, validation colors, menus, and dialogs on shared resources. Respect Windows high contrast and reduced motion. Keep artwork proportional instead of stretching it.

Accessibility acceptance criteria: all operations reachable by keyboard; logical focus order; visible focus; accessible names and help text; screen-reader announcements for progress/errors; labelled text with icons; status meaning conveyed beyond color. Test text contrast, disabled/selected states, keyboard-only matching, Narrator, window resizing, long translated/Unicode text, and 100/150/200% DPI including moving between monitors. These are test targets, not a claim of formal compliance certification.

Proposed shortcuts: Ctrl+O add files, Ctrl+A select files within the grid, Ctrl+S save staged changes, Ctrl+Z undo the current edit where appropriate, Delete remove selected rows from the workspace, and Escape cancel the current search/review. Avoid overriding text-editor shortcuts globally. Do not clear pending edits just because selection changes; offer Save/Discard/Cancel when closing a dirty workspace.

**Execution order and completion criteria**

| Phase | Deliverable | Exit condition |
| --- | --- | --- |
| 0. Preparation | Preserve original app in Git history, establish the modern project alongside it, pin SDK and packages, add ignore rules | Fresh restore/build works without tracked `bin`, `obj`, packages, personal files, or signing keys |
| 1. Writer and UI proof | ATL versus TagLib fixture comparison; WPF themed grid/editor prototype; self-contained publish smoke check | Core formats preserve audio and unrelated metadata; editor works with keyboard, DPI, and themes; writer/framework choice recorded |
| 2. Offline editor | Import, multi-select, Basic/Details/Artwork/Advanced fields, validation, staged diffs, safe writes and restore | Manual edits work without network; failed writes and undo are tested on disposable files |
| 3. Catalog integration | Async album search/lookup, storefront, source details, field mapping, throttling, cancellation | Works against saved test responses plus an optional live smoke check; null/partial/error responses are useful to the user |
| 4. Album workflow | Assignment engine, review/correction UI, bulk field policy, one-action save | Ambiguous and multi-disc fixtures pass; no file receives another track's metadata without review |
| 5. Release candidate | UI/accessibility polish, remaining format gates, reproducible packaging and local demo | Automated checks pass and the app launches locally for your hands-on review |
| 6. Publish after your approval | Push reviewed changes, update README demo, publish release assets | GitHub code, documentation, checksums, and binaries identify the same tested version |

Essential failure tests include blank numeric fields, comma-containing names, Korean/Japanese/accented text, missing Apple properties, album/track date differences, incomplete album responses, locked/read-only/corrupt files, duplicate paths, stale input snapshots, disk-space failure, interrupted staging/commit recovery, and cancellation. Test an unknown non-URL custom tag and extra artwork surviving a title-only edit, plus the retained-tag website-URL cleanup cases above during iTunes tagging. Live API availability must not make the normal automated test suite flaky.

Target **maintained Windows 11 releases on x64** first. Validate the current local 25H2 machine and a clean Windows test environment. ARM64 is a separate artifact and test target; a successful cross-publish is not execution validation. Decide and document any Windows 10 support only after checking runtime support and testing that OS; do not advertise it based solely on framework minimum versions.

**Builds, installer, GitHub release, and README recording**

Publish a self-contained Release build so end users do not need to install the .NET runtime separately. Provide a portable ZIP with the application EXE and necessary notices/resources, plus `ThirdAlien-Setup-<version>-win-x64.exe`. Pursue a single-file portable EXE after validating extraction behavior, settings paths, and library redistribution requirements; a portable folder is a valid first artifact, but should not be described as a single-file EXE. .NET single-file builds are architecture-specific. [Microsoft single-file deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview)

For WPF, leave trimming and Native AOT off unless the framework's documented constraints and our complete test coverage justify a change. WPF currently has documented trimming incompatibilities. [Microsoft trimming limitations](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/incompatibilities)

Use an Inno Setup script for the requested Windows installer EXE, with install/uninstall, shortcuts, stable application identity, and upgrades that preserve settings. [Inno Setup](https://jrsoftware.org/isinfo.php)

Keep portable settings beside the app only in an explicit portable mode with a writable directory; otherwise use the user's local application-data directory. Installer uninstall must leave music and recoverable backups alone. If TagLibSharp is selected, account for its LGPL redistribution/replacement requirements when deciding whether to bundle its DLL inside a single executable. Include applicable licenses and source/relinking information; the app's MIT license does not erase dependency obligations.

Signing requires a legitimate current signing identity; do not reuse the committed temporary certificates. An unsigned test build is possible and should be labelled accurately. Do not promise that an unsigned or newly signed executable will bypass Windows reputation prompts. Resolve release branding and icon rights as well; the existing Apple/iTunes-branded icon should be replaced with an app-owned identity.

Build and test locally first. Prepare release notes, checksums, license notices, install instructions, supported-format limits, and binaries before seeking final publication approval. Your stated gate is a working local app that you have reviewed; do not push or publish before you say it is good to go. Do not treat the initial modernization request as that later approval.

After the workflow is stable, screen-record the **actual running app** using disposable demo files and owned/licensed visual assets. Show import → album search → corrected match → tag edits → batch save → verified result, plus a brief theme switch. Frame the app window to exclude personal paths and notifications. Keep a good-quality source recording and generate an optimized GIF using an FFmpeg palette; aim for roughly 20–40 seconds, a readable 800–1000 pixel width, and a modest frame rate. Add a relative README image with useful alt text, and publish the recording/GIF together with the approved documentation update. Do not replace the demo with a simulated animation.

**Model handoff and current status**

The requested order is GPT-6 review/planning, then **GPT-5.6 Terra with high reasoning** for implementation. The active agent has no tool to switch this conversation's model. Select that model and reasoning setting in the client before starting execution; this document and the code review are the implementation handoff. No substitute model has been used to implement the application.

Completed here: original-code review, current-source research, public iTunes API probe, existing-binary startup smoke check, and these documents. Pending: the rebuild, local acceptance of the rebuilt app, publishing, demo recording, installer, and portable release. No source-code migration or GitHub publication has been performed.
