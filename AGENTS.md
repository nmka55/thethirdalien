# The Third Alien - project memory and working agreement

This file is the durable record of the user-approved product scope. It is derived from the full project conversation, not just the latest request. Future agents must read it before changing the project.

## Working rules

- Treat a new user message as an addition, correction, or reprioritization of this active project unless it explicitly cancels prior scope.
- Keep every requested item in the acceptance checklist. Do not declare a request complete because one visible item was changed while related requirements are still open.
- Explain the result, the reason for the change, how it was verified, and any remaining limitation. Never represent a code change as a verified real-file or visual fix without that verification.
- Build the Release solution and run relevant tests before reporting implementation work. For UI work, launch the app and inspect all relevant states: no selection, selected file, checked album file, light theme, dark theme, minimum practical window size, and keyboard use.
- Audio writes must use the temporary-copy, reopen-and-verify, backup-preserving path. A failed write must leave the original unchanged.
- Do not push, create a GitHub Release, upload a GIF, publish a build, or contact anyone until the user has reviewed the concrete result and explicitly approved that external action.
- Keep this file updated: move only fully implemented and verified work to **Verified**. Keep code candidates under **Open verification**.

## Product intent and reasons

| Requirement | Why it matters | Acceptance condition |
| --- | --- | --- |
| Preserve the original product idea: a lightweight local audio metadata fetcher/writer using iTunes, rather than a full replacement for MP3Tag. | The original MVP works and has personal value; the goal is to modernize it without losing its simple purpose. | The rebuilt application finds an album, lets the user review data, and safely writes local tags. |
| Rebuild from the ground up for current Windows standards and current OS compatibility. | The repository had not been updated for about a decade and began as a second-year university project. | SDK-style .NET projects, supported desktop framework, safe dependencies, current documentation, and distributable builds. |
| Prefer WPF/.NET 10 for this app; understand that `.sln`/`.slnx` are solution formats and UWP is not the desired new-app direction. | The user asked what the modern Windows standard is and wants a maintainable desktop editor. | One modern solution, WPF UI, MVVM-oriented state, and a documented rationale. |
| Keep Apple catalog integration factual and precise. | A wrong album or non-music result can damage a music collection. | Search results are strict music album collections; no broad artist merge, podcast, or raw-track rows are shown as album results. |
| Match as much legitimate song metadata as Apple exposes, but never claim that account, purchase, owner, DRM, or unavailable data can be reproduced. | The user wants a tagged local file to resemble a purchased iTunes file in song/album metadata only. | Map supported music facts; document unavailable fields and do not fabricate them. |
| Make the UI dark-theme, light-theme, accessible, compact, and easy to use. | The current UI had unreadable dark controls, oversized/misaligned controls, hidden actions, missing selection feedback, and clipped editor fields. | The UI works in System/Light/Dark, text is readable, selected items are obvious, and controls remain usable when resized. |
| Finish the entire requested backlog before presenting the work as done. | The user explicitly reported frustration that prior work addressed only one item from a list. | This file shows all requests, their reason, status, and verification evidence. |

## Detailed conversation-derived requirements

### Architecture, review, and delivery

- Review the legacy code and report what design choices were sound, what was weak, and how it should improve. Keep the review in `docs/CODE_REVIEW.md`.
- Produce and execute a modernization plan in `docs/MODERNIZATION_PLAN.md`; start by obtaining the required SDK.
- The user requested GPT-6 for analysis/planning and a GPT-5.6 Terra High execution phase. Honor that preference when the active environment exposes an appropriate model-selection control; do not claim a model switch that did not occur.
- Run the application locally during the rewrite so the user can inspect it.
- Add meaningful tests for rewritten behavior, then update the README with rich user/developer information.
- Only after user review: push to GitHub, create a release with installer/portable executable, record the program workflow, make a GIF, and add it to the GitHub README.

### Apple iTunes catalog behavior

- Determine whether the iTunes Search/Lookup API remains available and record live evidence in `docs/evidence/`.
- Audit the returned fields individually: distinguish real song/album metadata from store/catalog data. Document what can be written to file tags and what must not be written (prices, currency, storefront, preview URL, product/view URLs, streamability, catalog IDs, censorship variants, and similar store information).
- The legacy query was more precise: `media=music`, `entity=album`, `limit=70`, `explicit=no`, `country=us`. Preserve the important result-domain restriction. Do not repeat the regression that merged all song results and artist lookups into the album table.
- Support a narrowly validated exact-title fallback only if Apple omits a well-known album from the album search. It may surface an album collection derived from an exact `collectionName`; it must not surface unrelated raw tracks.
- Validate searches with real cases, including Lorde - *Pure Heroine*, and explain discrepancies as Apple storefront/catalog ranking behavior rather than silently broadening results.
- Use the largest verified Apple artwork request available for the chosen album. The user requested 600 x 600 artwork so local tagged files resemble iTunes releases where legitimate metadata permits it.
- Do not offer BPM when the public iTunes data does not supply it. Remove the Comment field from the normal workflow and use a correctly labelled **Unsynchronized lyrics** field instead.

### Metadata editing and safety

- Support more file extensions than the original MP3/M4A app when the tag library can reliably handle the format. Present format support and limitations honestly.
- Read and expose the normal/popular tags in the main editor: title, track artist, album, album artist, genre, composer, release year, track/disc number and totals, and unsynchronized lyrics.
- Explain that ISRC is a real recording identifier and Website is a real optional URL tag. Neither is an iTunes Search field, so remove both from the normal iTunes-driven editor. If they exist in a file, make them visible in Full tags rather than silently discarding them.
- When Apple does not provide a tag and the retained local field value is a website URL, clear only that URL under the agreed conservative URL-cleaning rule. Preserve non-URL text, emails, filenames, and uncertain values.
- Add **Full tags** at the bottom of Edit selected file. It opens a small dialog with Tag name and Value columns; Add creates a row; selecting a row enables Edit; Edit makes both cells editable; Save writes and verifies the tag values. Preserve and show already readable additional fields. Explicit clear/delete behavior remains a product decision to implement and test.
- Never overwrite a file in place before verification. Each write must make a nearby backup, verify the temporary copy, then replace the original.

### Artwork requirements

- The Edit selected file heading and explanatory subtitle must appear above the artwork component.
- The artwork component must be genuinely square, horizontally centered, within right-panel padding, and followed by a readable dark-gray `Selected artwork: 600 x 600 request` caption with 8px spacing.
- When there is no selected file or no embedded cover, show a clear, non-corrupted placeholder inviting the user to choose an image.
- When an imported-file row is selected, show that file's actual embedded artwork if present.
- Clicking the artwork component with a selected file must open an image picker, write the chosen image as front-cover artwork, reopen/verify it, and display the result. With no selected file, show a clear selection-required status instead of failing silently.
- Verify this workflow with real MP3 and M4A files; code presence alone does not count as completion.

### Main UI requirements

- Keep the Edit selected file panel as the right third of the main workspace beside the tables, full-height, padded, scrollable, and usable at narrow widths.
- Fix input colors in Dark mode, table-header visibility in Dark mode, selected row highlighting, and the Theme picker height/alignment.
- Fix the Find an album input: it must be a compact text box with vertically centered text, not a tall control whose text floats.
- Add action-appropriate icons from a compatible open-source library for buttons.
- Generate and use a modern, distinguishable custom app icon. It must remain visually legible and appropriately sized in the Windows taskbar, and be documented in the README.
- Imported file table behavior: its first column is a checkbox; clicking the checkbox or choosing a row checks/highlights it; the header checkbox selects or clears all rows.
- Status bar content must report selection/staging/save counts in the form `X of Y selected | Z staged | N saved`.
- Keep the search-results **Load tracks** action accessible when the window is small; freezing the action column is acceptable.

### Album batch matching requirements

- Let the user select multiple imported files belonging to one album, search Apple for the album, choose the right edition, and load the album track list.
- The matching dialog must display Apple tracks sorted ascending by disc/track number on the left and selected local files on the right.
- The up/down move controls belong between the two tables and use arrow icons.
- The local-file table includes a `#` column. Moving a file changes its placement/order number as displayed.
- Both sides must have the same number of rows. Do not silently lose selected files or tracks. Use explicit blank counterpart rows if counts differ.
- Before saving, make unmatched rows clear. Write only confirmed file/track pairs and report saved/skipped/failed counts.
- Investigate a prior defect where five selected songs appeared as only two mapping rows. The former `Zip` behavior was identified as a silent truncation cause, but real five-file testing is required.

### Documentation, distribution, and demo requirements

- README must explain what the program is, how it works, features, libraries and exact versions, Windows/runtime compatibility, file support, privacy/API use, metadata boundaries, build/test/run instructions, custom icon, and useful developer details.
- Keep README claims accurate. Do not say batch, custom tags, real format support, screenshots, installer, or GIF are complete unless they have been verified and delivered.
- Produce both an installable Windows executable and a portable executable, suitable for GitHub Releases, after the user reviews the application.
- Record the program workflow, convert it to a GIF, add it to README, then push/upload only after explicit approval.

## Active acceptance checklist

### Open implementation

- [ ] Complete the iTunes-field audit and metadata mapping document using the saved live evidence.
- [x] Batch writes now download the selected album artwork once and apply it with metadata in one verified transaction; disposable MP3/M4A round trips are recorded in `docs/evidence/verification-2026-09-11.md`.
- [x] Batch writes now apply the conservative URL cleaner to retained Composer, Lyrics, and Website fields; broader custom-field cleanup/preview remains open.
- [ ] Add explicit unmatched-file/track review and saved/skipped/failed reporting to album batch writes.
- [ ] Add per-format support documentation backed by real read/write tests.
- [ ] Decide, implement, and test clear/delete behavior in Full tags.
- [ ] Produce installer and portable release artifacts after user review and approval.
- [ ] Record the workflow GIF, update README, push, and publish GitHub Release after user review and approval.

### Open verification

- [x] Live strict search verification for Lorde - *Pure Heroine* is recorded in `docs/evidence/verification-2026-09-11.md`.
- [ ] Test five selected files in the album-mapping dialog and confirm all are visible, ordered, and written only when paired.
- [x] Disposable MP3/M4A Full tags standard/custom-field round trips are recorded in `docs/evidence/verification-2026-09-11.md`.
- [x] Disposable MP3/M4A artwork save/reopen verification is recorded in `docs/evidence/verification-2026-09-11.md`; WPF visual picker acceptance remains open.
- [ ] Test no-selection/selected/no-artwork states in the editor.
- [ ] Test System, Light, and Dark themes for inputs, headers, selected rows, artwork placeholder, Theme picker, and search input.
- [ ] Test the main layout at practical minimum window width and keyboard navigation.
- [ ] Test checkbox/header-checkbox behavior and status-bar counts.

## Implemented candidates awaiting the verification above

- .NET 10 WPF solution with Core, Infrastructure, App, and test projects.
- ATL-based metadata reads/writes and backup-and-verify commit service.
- Strict album-only catalog client with exact-title-only fallback.
- System/Light/Dark resource set, selected-row styling, button icons, and custom application icon.
- Main editor using unsynchronized lyrics instead of BPM/Comment; Website and ISRC removed from the normal form.
- Full tags dialog with Add, select-to-enable Edit, and Save through the verified tag service.
- Square artwork selector with placeholder and image picker command.
- Numbered album-mapping rows designed to avoid `Zip` truncation.
- Rewritten README with current features, dependencies, compatibility, and limitations.

## Current code conformance review

Read [docs/IMPLEMENTATION_CONFORMANCE_REVIEW.md](docs/IMPLEMENTATION_CONFORMANCE_REVIEW.md) before implementing or reporting on the current UI/tagging behavior. It contains the source-backed repair order, including the artwork state bug, corrupted status text, and unsafe batch error handling.
## Requirements fulfillment audit

Read [docs/REQUIREMENTS_AUDIT.md](docs/REQUIREMENTS_AUDIT.md) before marking any scope item complete. It classifies every user request as Verified, Partial, Open, or Approval-gated and identifies the requirements that were previously reported too early.
## Current evidence baseline

- Live iTunes query evidence is stored under `docs/evidence/`.
- The most recently verified code sequence is `dotnet build ThirdAlien.slnx -c Release` and `dotnet test ThirdAlien.slnx -c Release`; 14 tests passed. Real disposable MP3/M4A evidence is in `docs/evidence/verification-2026-09-11.md`.
- The WPF app has been launched locally. This is a smoke check only; it does not satisfy the real-file and visual acceptance checks listed above.