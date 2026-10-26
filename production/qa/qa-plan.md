# QA Plan — Eight Months of Silence (vertical slice)

**Scope**: MVP + vertical slice per `docs/10-production-scope.md`
(Bab 1 + 1 segmen rumah Adrian, target 15–25 menit).
Scene kanonis: `PunyaPandu.unity` (buildIndex 0).
**Status dokumen**: strategi + kriteria. Hasil eksekusi dicatat di
`smoke-<tanggal>.md`; sesi manusia di `production/playtests/`.

## Rujukan desain (GDD-001)

- Aturan sistem: `docs/03-core-gameplay-systems.md` (core loop, movement,
  inspection, evidence, inventory + anti-frustrasi, puzzle, survival/combat)
- HUD/input: `docs/07-ui-ux.md` (HUD minimal, semua opsi togglable, baseline input PC)
- Scope: `docs/10-production-scope.md` (MVP → vertical slice; daftar di-luar-scope)
- Risiko & DoD pengalaman: `docs/11-risks-and-dod.md` (7 risiko, checklist DoD)
- Verifikasi teknis: `agent/verification-matrix.md` (baris per area + gerbang universal)

## Tingkat pengujian

| Tingkat | Sarana | Cakupan |
|---|---|---|
| EditMode (NUnit) | `Assets/_Yumiwa/Application/Tests/`, `Presentation/Tests/` (26 file, tanpa assembly PlayMode) | Aturan Domain + orkestrasi Application dengan fake |
| Smoke live (agent) | Unity MCP Play+Execute di `PunyaPandu` | Semua mekanik slice end-to-end, console bersih, screenshot |
| Playtest manusia | `production/playtests/session-*.md` | Pengalaman pemain baru, mid-game, kurva kesulitan, fun/confusion |

## Kriteria masuk (boleh uji)

- Kompilasi 0 error/warning (`get_compilation_errors`)
- Scene aktif `PunyaPandu`, misi auto-start tanpa exception

## Kriteria keluar smoke (PASS)

1. Semua cek `smoke-checklist.md` PASS — tidak ada NOT_RUN pada baris wajib.
2. Console: 0 `MissingComponentException` / `ArgumentException` / `NullReferenceException`.
3. Play Mode di-stop; scene kembali bersih (`isDirty:false`).
4. Laporan `smoke-<tanggal>.md` ditulis dengan durasi, aksi, dan bukti.

## Kriteria keluar QA tahap ini (siap gate ulang)

- 1 smoke PASS + 3 sesi playtest manusia (baru, mid-game, kesulitan) + suite EditMode run tercatat.
- Verdict: PASS / CONCERNS / FAIL mengikuti `gate-check` Production → Polish.

## Pemetaan risiko → cek (`docs/11-risks-and-dod.md`)

| Risiko | Cek smoke/playtest |
|---|---|
| Inspection jadi gimmick | Hotspot → fakta di Evidence Log; solve butuh prasyarat |
| Inventory → soft-lock | Critical/optional tidak makan slot; objective drop → auto-return |
| Movement lambat | Walk/sprint/crouch/jump + stamina; feel tetap responsif |
| Terlalu banyak teks | Clue visual + prompt kontekstual terbaca di screenshot |
| Detective vs action | Combat situasional (ammo 7, situasional), bukan arena-clear |
| Gore vs usia | Implisit; di luar cek mekanik — catatan manual |
| Scope membesar | Setiap temuan dicek lawan `docs/10` sebelum jadi task |

## Yang TIDAK diuji di sini (sengaja, plus alasan)

- Tulis save ke disk di Play Mode — menghindari polusi `persistentDataPath`
  pemain; tercakup `SaveRepositoryTests` (temp-dir).
- Input fisik tombol-per-tombol — smoke memakai service API + verifikasi presenter
  ada/terhubung; uji input penuh milik playtest manusia.
- Final encounter/Adrian — 0 implementasi + eskalasi `AGENTS.md` §8; di luar slice ini.
- Angka balancing final — seluruhnya `<!-- TBD#6 -->` per `docs/12-references-and-open-tbd.md`.

## Kepemilikan

- Smoke live: agent (laporan per file `smoke-<tanggal>.md`).
- Playtest: manusia (template `playtests/playtest-template.md`).
- Penentu DONE: manusia (status agent maksimum HANDOFF).
