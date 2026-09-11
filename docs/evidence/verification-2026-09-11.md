# Verification evidence - 11 September 2026

All audio files used here were disposable one-second sine-wave files generated in the system temporary directory. No personal music files were opened or changed.

## Strict Apple search regression

Live requests used the legacy-compatible strict query settings:

- `term=pure heroine`
- `media=music`
- `entity=album`
- `limit=70`
- `country=US`
- `explicit=no`

The album endpoint returned zero exact `Pure Heroine` album-name matches. The narrow fallback queried `entity=song` and accepted only a song whose `collectionName` exactly equalled `Pure Heroine`. It returned Lorde collection ID `1440818584`.

## Real tag and artwork round trips

For both a generated MP3 and M4A file, ATL successfully completed:

1. standard title/artist metadata write and readback;
2. 600 x 600 PNG front-cover write and exact-byte readback;
3. Full tags title write and custom `TESTCUSTOM` field write/readback;
4. nearby backup creation for every verified commit.

ATL reads an M4A front cover back as `Generic` rather than `Front`. The tag service now treats either classification as the front-cover artwork, while replacing either existing classification on write.

## Live Apple artwork embedding

The app looked up Lorde - *Pure Heroine* (collection `1440818584`), downloaded its requested 600 x 600 artwork through the catalog provider, and embedded 69,903 bytes into a disposable MP3 in the same combined metadata/artwork safe-commit path. Readback bytes matched and a backup was present.

## Boundaries

These checks verify service behavior, not the WPF visual workflow. The remaining acceptance checks are still the UI theme/layout matrix, imported-file selection/checkbox behavior, five-file mapping workflow, and per-format preservation coverage beyond MP3/M4A.