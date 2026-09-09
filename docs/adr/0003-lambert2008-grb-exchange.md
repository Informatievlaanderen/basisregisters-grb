# 3. Exchanging Lambert 2008 with GRB and building-registry

Date: 2026-09-09

## Status

Accepted

## Context

The Flemish base registries are moving from Lambert 72 (EPSG 31370) to Lambert 2008 (EPSG 3812).
building-registry has already done its side of the exchange: its ADR 0003
([building-registry#1385](https://github.com/Informatievlaanderen/building-registry/pull/1385)) made the
BackOffice API accept either reference system on GML input, normalizing to whatever its event store
holds, and its ADR 0007 covers transforming that event store. All five GRB endpoints — measure, change
measurement, correct measurement, realize-and-measure, change outline — run their incoming GML through
`GmlGeometryNormalizer.ToEventStoreSrs`, and `GrbGmlPolygonValidator` accepts both `srsName`s.

This repository is the last link that could not. It sits between two parties that are both becoming
bilingual and was itself hardcoded to one language.

### Where the reference system matters

The pipeline has two halves, and the reference system matters at each end of it:

1. **Upload.** A zip of `gebouw_ALL.shp` / `.dbf` / `.shx` lands on S3. `UploadProcessor` validates it
   (`ZipArchiveValidator`), then translates it (`GrbShapeRecordsTranslator` →
   `GeometryTranslator.ToGeometryPolygon`) into `JobRecord.Geometry`, a `sys.geometry` column.
2. **Process.** `JobRecordsProcessor` → `GrbDataMapper.Map` → GML → `BackOfficeApiProxy` →
   building-registry.

`GrbDataMapper.GetGml` wrote `srsName="https://www.opengis.net/def/crs/EPSG/0/31370"` unconditionally,
whatever the coordinates were. That is the sharper of the two problems: the moment GRB switches,
building-registry faithfully treats Lambert 2008 coordinates as Lambert 72 and converts them, putting
every building roughly 500 km from where it is. Nothing errors — the GML stays well-formed, the numbers
stay plausible, and the failure is silent all the way through.

### Nothing in a GRB delivery declares its reference system

- Neither delivery contains a `.prj`. Both are `.shp` / `.dbf` / `.shx` only.
- The **filename does not survive the upload.** `UploadPreSignedUrlHandler` issues a pre-signed POST for
  the fixed key `Job.UploadBlobName` (`upload_{jobId}`), and `UploadProcessor` reads
  `Job.ReceivedBlobName` (`received/{jobId}`). Whatever the operator called the file locally never
  reaches this code, and both test archives carry identical inner entry names. The `_LB08` suffix
  distinguishes two fixtures in the repository; it cannot signal anything in production.

What *is* unambiguous is the coordinates. The two Flanders envelopes are roughly 500 km apart and do not
overlap on either axis:

| reference system | x range |
|---|---|
| Lambert 72 (31370) | 21 492 – 259 366 |
| Lambert 2008 (3812) | 521 398 – 759 275 |

So a real building falls in at most one of them, and neither system's coordinates can be mistaken for
the other's. parcel-registry decides the same question the same way, and for the same reason: GRB
geometry that arrives carrying no trustworthy label (its ADR 0005,
`GeometryReferenceSystem.ReferenceSystemOfCoordinates`).

## Decision

Detect the reference system from the coordinates at upload, record it on the row, and read it back when
writing the GML. Refuse an archive that cannot be placed, or that disagrees with itself.

### The srsName follows the geometry

`GrbDataMapper.GetSrsName` derives the attribute from `geometry.SRID` rather than a constant. A geometry
carrying SRID 3812 is declared as Lambert 2008; anything else is declared as Lambert 72.

Falling back rather than switching on the exact value is deliberate. Every job record written before the
upload started stamping an SRID holds 0, and those rows are Lambert 72 by definition — the same rule
`BuildingRegistry.GeometryReferenceSystem.ReferenceSystem` applies to SRID-less event store geometry.

The `https` scheme in the emitted URI is kept as it was. building-registry accepts either scheme (its
ADR 0003), so this could have been changed to the `http` form that
`SystemReferenceId.SrsNameLambert72` holds, but there is no reason to alter the outgoing payload beyond
what this change requires.

### The SRID is detected at upload, not read from configuration at send time

A feature toggle would be the obvious mechanism and is the wrong one here. Jobs are queued: uploaded
once, processed later, deliberately outside office hours (`OutsideOfficeHoursOptions`). A toggle read
when the job is *sent* mislabels every Lambert 72 job still sitting in the queue the moment it flips,
which is precisely the window in which a mistake is most likely and least visible.

The answer therefore has to travel with the row. `JobRecord.Geometry` is `sys.geometry`, which already
stores an SRID, so there is nowhere new to put it and no schema change:

- Nothing does a spatial query on that column. `Overlap` comes from the dbase `TPC` field, not from a
  computation.
- `JobRecordsArchiver` copies the column verbatim.

The SRID is free to carry the answer.

### Classification lives in one place

`GrbGeometryReferenceSystem.Of(Geometry)` returns the SRID the coordinates fall in, or `null` when they
fall in neither envelope. Both the validator and the translator call it rather than each testing
envelopes of their own — mirroring how building-registry keeps `GeometryReferenceSystem` as the single
place that answers this question.

### Two new rejections

`GrbShapeRecordsValidator` already walked every shape record. It now classifies each polygon and reports
two new `ValidationErrorType` members through the existing per-record machinery (grouped by type into a
`FileError` with a `recordnumbers` parameter, in `ZipArchiveValidator`):

- **`GeometryOutsideFlanders`** — the record is in neither envelope, so there is no saying what its
  coordinates mean. Refused rather than guessed at, for the reason above: a wrong guess is silent.
- **`MixedReferenceSystems`** — the archive holds records in both. GRB converts a delivery as a whole, so
  a mix means something went wrong upstream, not that the archive needs interpreting record by record.
  The records in the **minority** are the ones flagged, so the operator is pointed at the handful that
  disagree rather than at the whole file.

Neither needs a new exception type or a Dutch message: unlike the archive-level
`DbRecordsWithMissingShapeException` path, per-record errors surface as the enum name plus the record
numbers.

Validation runs before translation in `UploadProcessor`, so a mixed or unplaceable archive is rejected
before a single `JobRecord` is written. That is what lets `GrbShapeRecordsTranslator` stamp
`GrbGeometryReferenceSystem.Of(geometry)` without re-deciding anything.

## Consequences

- **Nothing changes while GRB still delivers Lambert 72.** Every existing delivery classifies as 31370,
  gets stamped 31370, and is declared as 31370 — the same bytes as before. The outbound half in
  particular is inert until something upstream changes, which is why it was committed first.
- **GRB can switch unilaterally.** No configuration change, no deploy, and no coordination window: the
  first Lambert 2008 delivery is detected, stamped and declared correctly on arrival, and jobs already
  queued in Lambert 72 keep their own labels.
- **A delivery that is neither, or both, now fails the upload** instead of reaching building-registry.
  This is a new way for an upload to be rejected. It should never trigger on real GRB data, and if it
  does, the failure is the point.
- **The reference system is decided from data, not from a declaration.** This is safe only because the
  envelopes do not overlap. It is not a technique to reach for where the two candidate systems could
  produce coordinates in the same range — a test asserts the non-overlap the approach rests on.
- **`gebouw_ALL_LB08.zip` does not exercise the full upload flow.** Its shape entry is genuine GRB
  Lambert 2008 geometry and is used as such, but its dbase entry was exported with a different schema
  than `GrbDbaseSchema` requires (`GRBIDN` 10 rather than 9; `GVDV`, `GVDE` and `GRID` 254 rather than
  10, 10 and 128), so `ZipArchiveDbaseEntryValidator` refuses it for reasons unrelated to the
  coordinates. End-to-end upload coverage therefore still runs on `gebouw_ALL.zip`. Re-exporting that
  fixture with the GRB dbase schema would close the gap.
- **This repository now depends on `Be.Vlaanderen.Basisregisters.GrAr.CrsTransform`**, for
  `IsInsideFlandersUsingLambert72` / `IsInsideFlandersUsingLambert08`.
