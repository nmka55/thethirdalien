# The Third Alien — review of the original application

Reviewed 11 September 2026. Baseline: commit `d23e5b1` (`another readme edit`, 20 February 2021). The README dates the original application to summer 2016.

**Assessment**

You chose a useful, achievable product: find an album, identify a song, and save metadata into a local file. Using a catalog API and a dedicated tag library was sound engineering. The main limitations are the boundaries between the UI, network, and disk, rather than the underlying idea. This is a working prototype worth rebuilding with a stronger data model and safer writes.

The highest priority is reliable editing of somebody's music collection. A modern theme will improve the experience, but correctness, preservation, and recovery must come first.

**What I inspected and verified**

- All handwritten C# application files; the main form's designer; project, package, application, and manifest configuration; tracked files and README. No applicable `AGENTS.md` was found.
- The application is Windows Forms on .NET Framework 4.5.2, built with the old MSBuild project format. It references TagLib# 2.1.0.0 and a locally copied Newtonsoft.Json assembly, version 9.0.0.0.
- The working tree was clean before this review. The repository tracks debug executables, `bin`, `obj`, restored packages, a user-specific project file, and two temporary signing `.pfx` files.
- Launched the existing tracked `WindowsFormsApplication1/bin/Debug/The Third Allien.exe`. Its main window appeared and the process reported `Responding = true`. This was a startup smoke check of the existing binary, not a fresh build or a full functional test. The binary has not been proven identical to the checked-in source.
- Local Windows reports build 26200 and version 25H2. `dotnet --info` finds .NET 8.0.25 runtimes, but **no .NET SDK**. `ffmpeg` is available; `msbuild` and `gh` were not found on PATH. An SDK is needed for the new application.
- Live HTTPS requests to iTunes search and lookup succeeded without credentials. Searching for Daft Punk's *Discovery* found collection `697194953`; lookup returned one album and 14 songs. See [API evidence](evidence/itunes-probe-2026-09-11.json).
- No music files were opened or modified. No application source was changed, and nothing was committed, pushed, recorded, or published.

**Where you were right**

| Decision | Why it holds up |
| --- | --- |
| Delegate file tagging to TagLib# | Correctly avoided implementing binary media formats yourself. Keep this separation behind an application-owned interface. |
| Search albums, then look up a collection ID | This is the right basis for accurate album selection and batch tagging. The old code already requests the album's songs. |
| Use HTTPS | Both API endpoints already use encrypted transport. |
| Offer manual editing alongside search | Catalog data can be incomplete or refer to another edition. Manual edits are a necessary feature. |
| Include track/disc counts and artwork | You recognized that tagging is more than a title and artist. |
| Keep the application focused | The small feature set makes a clean rebuild practical. A database server, account system, or web backend is unnecessary for the core workflow. |
| Document limitations and use an open-source license | The README is candid about what works and what does not. Keep that honesty in the new compatibility matrix. |

Windows Forms, .NET Framework, and Newtonsoft.Json were reasonable choices in 2016. Their age is a migration consideration, not evidence that choosing them then was wrong.

**Findings, ordered by impact**

1. **High — writes are immediate and have no recovery mechanism.** [Form1.cs:87](../WindowsFormsApplication1/Form1.cs#L87), [Form1.cs:138](../WindowsFormsApplication1/Form1.cs#L138), and [finalResultTab.cs:101](../WindowsFormsApplication1/finalResultTab.cs#L101) write directly to the original file. Clear Tags immediately clears and saves. Success checks compare the in-memory title with the value just assigned; they do not reopen the file. A write exception, interruption, or library defect has no app-managed recovery path. Stage changes, show a diff, write a temporary copy, verify it, then replace the original with a backup and journal.

2. **High — track artist and album artist are conflated.** [Form1.cs:40](../WindowsFormsApplication1/Form1.cs#L40), [Form1.cs:96](../WindowsFormsApplication1/Form1.cs#L96), and [finalResultTab.cs:119](../WindowsFormsApplication1/finalResultTab.cs#L119) use `AlbumArtists` for the field labelled Artist and for Apple's song `artistName`. `Performers` is never assigned. This can leave an MP3's normal artist empty or stale and makes compilation albums incorrect. Model track artist and album artist separately; use the collection's artist only as a clearly identified album-artist candidate.

3. **High — selecting files, displayed rows, and the active tag object can disagree.** [Form1.cs:259](../WindowsFormsApplication1/Form1.cs#L259) clears `files` before the file dialog is accepted. Cancelling leaves the old rows displayed but clears the backing list. [Form1.cs:278](../WindowsFormsApplication1/Form1.cs#L278) clears rows without clearing `files` or `tagFile`; refresh can bring the rows back, and commands can still address the previously selected file. [Form1.cs:182](../WindowsFormsApplication1/Form1.cs#L182) clears the editor but retains that same object. Replace these parallel states with one bound file collection, stable file IDs, a selected item, and explicit command availability.

4. **High — input and normal file failures are not handled reliably.** Numeric conversions in [Form1.cs:93](../WindowsFormsApplication1/Form1.cs#L93) fail on blank, negative, non-numeric, or oversized values. Most handlers only catch `NullReferenceException` or `FileNotFoundException`; locked, read-only, corrupt, unsupported, inaccessible, and disk-full files are not covered. Validate before constructing a write plan and report errors per file. `FileIOPermission.Demand()` is not a substitute for handling actual I/O failures.

5. **High — network work blocks the UI and has incomplete failure handling.** [briefResult.cs:24](../WindowsFormsApplication1/briefResult.cs#L24) and [finalResultTab.cs:26](../WindowsFormsApplication1/finalResultTab.cs#L26) download and parse JSON inside form constructors. The latter also loads artwork synchronously inside the track loop. Slow requests can make the app appear hung; lookup, decoding, and image failures are mostly unhandled. Use asynchronous services with timeouts, cancellation, bounded retry, caching, and a visible loading/error state.

6. **Medium — catalog data is filtered and associated incorrectly.** [briefResult.cs:33](../WindowsFormsApplication1/briefResult.cs#L33) excludes explicit content, fixes the storefront to US, and later omits cleaned editions. Users cannot reliably choose the version they own. The deduplication at lines 41–51 reduces the ID list but then indexes the original results using that shorter list's positions. If duplicates occur, it can omit unrelated albums while retaining duplicates. Group whole album records by ID; make storefront and explicit-content filtering visible choices. Pass the same storefront to lookup.

7. **Medium — encoding and missing-field assumptions make searches brittle.** [keywordInputForm.cs:30](../WindowsFormsApplication1/keywordInputForm.cs#L30) replaces spaces with `+` but does not encode characters such as `&`, `+`, or `#`. Dynamic JSON and `.ToString()` calls on grid cells assume fields always exist. Use correct query encoding, typed nullable DTOs, and validation of both response type and required fields. Preserve Unicode names.

8. **Medium — artwork handling performs unnecessary work and can discard data.** [finalResultTab.cs:54](../WindowsFormsApplication1/finalResultTab.cs#L54) downloads the album image once per song, and assumes changing `100x100bb.jpg` to `600x600bb.jpg` will always work. [finalResultTab.cs:127](../WindowsFormsApplication1/finalResultTab.cs#L127) replaces every embedded picture with one cover. [Form1.cs:208](../WindowsFormsApplication1/Form1.cs#L208) converts images to JPEG on save and can retain metadata describing a different original image format. Fetch once per artwork URL, validate size/type, preserve original bytes where possible, and edit the front cover without silently removing other pictures. Artwork retrieval failure must not prevent a text-only edit.

9. **Medium — resource lifetimes are unclear.** Tag objects created in [Form1.cs:34](../WindowsFormsApplication1/Form1.cs#L34), [Form1.cs:55](../WindowsFormsApplication1/Form1.cs#L55), and [finalResultTab.cs:116](../WindowsFormsApplication1/finalResultTab.cs#L116) are not disposed. Images and memory streams also lack deterministic cleanup; `Image.FromFile` can retain an image-file lock. Scope file handles to each operation, release old images, and keep byte arrays or immutable snapshots in the UI. The impact of particular TagLib handles depends on the library implementation; a persistent lock was not reproduced in this review.

10. **Medium — file deletion is more destructive than removing a row.** [Form1.cs:166](../WindowsFormsApplication1/Form1.cs#L166) calls `File.Delete` after confirmation. It permanently deletes the music file; there is no Recycle Bin or undo integration. The list update also depends on the selected row remaining valid. For the rebuild, “Remove from list” should only remove the item from the workspace. Physical deletion is outside the requested core feature set.

11. **Medium — UI structure constrains usability and testing.** `Form1.passFile` is global state; forms reach through owner chains and retrieve business data from numbered cells. The main grid explicitly sets `MultiSelect = false` and `ReadOnly = true` in [Form1.Designer.cs:357](../WindowsFormsApplication1/Form1.Designer.cs#L357). Editor updates depend on a mouse-click handler rather than selection changes, weakening keyboard navigation. Several actions are image-only buttons, layout uses fixed sizes, and DPI awareness is commented out in the manifest. Move editing and matching into testable services/view models; use responsive layout, accessible labels, and keyboard-accessible commands.

12. **Medium — builds and releases depend on leftovers from one machine.** [WindowsFormsApplication1.csproj](../WindowsFormsApplication1/WindowsFormsApplication1.csproj) references Newtonsoft.Json from `bin/Debug/Net45`, tracks output assets as source, contains an absolute desktop publish path, enables signing with temporary keys, and uses different Debug/Release platform targets. There is no automated build/test workflow. Use SDK-style projects, NuGet `PackageReference`, a pinned SDK, ignored build output, and reproducible release scripts. Treat the tracked `.pfx` files as exposed signing material; their contents/passwords were not inspected. Do not reuse them. Removing files from the new tree would not remove their historical copies.

**Metadata mistakes to avoid repeating**

Splitting an artist on commas corrupts legitimate names such as `Earth, Wind & Fire`; Apple supplies a display string, not a structured contributor list. Retain it as one value unless the user explicitly adds separate people. Keep artist, album artist, composer, and lyricist distinct.

The live result exposed another subtle issue: the album date for *Discovery* was `2001-03-12`, while the first song's date was `2000-11-30`. The old importer takes each song's date and reduces it to a year. For album tagging, propose the selected album's release date, retain the track date in source details, and let the user choose. Neither is proof of the original recording date.

An API's duration, country, currency, price, streamability, and preview URL are catalog properties. They are not all audio tags. The local file's codec, duration, bitrate, sample rate, and channels must be measured from the actual file and shown read-only. Never overwrite these technical properties with catalog values.

**Dependency update assessment**

The original dependency has updates: the current published `TagLibSharp` package is **2.3.0**, dated July 2022, versus this repository's old `taglib` 2.1.0.0 package. Its release notes include an Opus write-corruption fix, illustrating why extending the old file dialog's filter alone would be inadequate. The package uses LGPL-2.1-only. [TagLibSharp package and release notes](https://www.nuget.org/packages/TagLibSharp)

For a ground-up application, compare it against **ATL.NET (`z440.atl.core` 7.16.0)**. The maintainer published release 7.16 on 5 August 2026; the project offers a unified audio metadata API and format-specific additional fields. It is an appropriate candidate for a broader editor, subject to our own preservation tests. [ATL package](https://www.nuget.org/packages/z440.atl.core/), [release](https://github.com/Zeugma440/atldotnet/releases/tag/7.16), [MIT license](https://github.com/Zeugma440/atldotnet/blob/master/LICENSE)

Recommendation: make ATL the initial rebuild candidate, compare both libraries on generated disposable fixtures, then ship one writer behind `IAudioTagService`. Do not route the same file through two writers during a normal save. Keep third-party notices and package provenance in release artifacts regardless of the choice.

**What this review does not establish**

The legacy executable's startup does not prove its tagging or search UI still works end to end. Static findings above are code-path analysis, not a claim that each failure was reproduced. There are no existing automated tests in the tracked tree. A new build, full metadata round-trip verification, accessibility testing, clean-machine installation, and ARM64 execution are work for the implementation phase.

The [modernization plan](MODERNIZATION_PLAN.md) defines that phase, including metadata scope, album matching, visual design, delivery, and acceptance criteria.
