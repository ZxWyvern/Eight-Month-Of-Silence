# Smoke Checklist — vertical slice (`PunyaPandu`)

Cara pakai: jalankan berurutan dalam satu sesi Play Mode via Unity MCP.
Isi kolom Hasil per baris: PASS / FAIL / NOT_RUN + bukti singkat.
Hasil sesi penuh dicatat di `smoke-<tanggal>.md`. Rujukan: `qa-plan.md`.

## A. Boot & kebersihan

| # | Cek | Kriteria PASS |
|---|---|---|
| A1 | Kompilasi sebelum Play | 0 error/warning |
| A2 | Scene aktif | `PunyaPandu`, buildIndex 0 |
| A3 | Misi auto-start | Log `Mission: M01_Alley`, `CurrentIndex` valid, tanpa exception |
| A4 | Kompilasi sesudah Stop | 0 error/warning, scene `isDirty:false` |

## B. Player & HUD (`docs/03` movement, `docs/07` HUD)

| # | Cek | Kriteria PASS |
|---|---|---|
| B1 | Movement state | `Idle`, `Grounded=True` saat spawn |
| B2 | Stamina/Health/Flashlight | stamina 100, HP penuh, baterai 100, lampu sesuai status `IsOn` |
| B3 | Flashlight toggle | On↔Off mengubah `IsOn` tanpa error |
| B4 | Presenter HUD | Movement/Inventory/Health/Stamina/Mission/Evidence/Prompt presenter ada & enabled |

## C. Inventory & recovery (`docs/03` anti-frustrasi)

| # | Cek | Kriteria PASS |
|---|---|---|
| C1 | Slot awal | 6 slot kosong, kedua tangan kosong |
| C2 | Tambah tool | `TryAdd crowbar=True`, tanpa `rejectReason` |
| C3 | Tukar tangan | `SwapHands` / `QuickSwapToHand` berhasil tanpa reset visual |
| C4 | Objective drop → recovery | `DropWithRecovery` untuk item objective → callback restore terdaftar, item kembali terlihat |
| C5 | Batas 6 slot | Slot penuh menolak item ke-7 dengan alasan di UI |

## D. Evidence & inspection (`docs/03` aturan evidence)

| # | Cek | Kriteria PASS |
|---|---|---|
| D1 | Log awal kosong | 0 entri saat spawn |
| D2 | Tambah critical | `AddEvidence watch_bloody` tercatat, TIDAK memakai slot fisik |
| D3 | World items | Semua `WorldInspectableItem` `active` + `IsAvailable=True` |
| D4 | Hotspot survive Cancel | Hotspot valid tetap terdiscover setelah Cancel (uji manual/inspection) |

## E. Puzzle & misi (`docs/03` investigasi/deduksi)

| # | Cek | Kriteria PASS |
|---|---|---|
| E1 | Gate prasyarat | Tanpa prasyarat: `Locked`, `CanAttempt=False` |
| E2 | Buka dengan prasyarat | Setelah evidence+item: `CanAttempt=True`, state `Solvable` |
| E3 | Jawaban salah | `False` + feedback logis + tanpa progress-loss |
| E4 | Jawaban benar | `True`, state `Solved`, event solve tepat 1×, solve-ulang ditolak |
| E5 | Konsekuensi misi | Solve memicu advance misi (M01→M02) via event |
| E6 | Advance manual | `Advance()=True`, index bertambah, dapat di-restore |

## F. Trap (`docs/03` survival: observe → hindari)

| # | Cek | Kriteria PASS |
|---|---|---|
| F1 | Siaga awal | `IsArmed=True` sebelum interaksi |
| F2 | Peringatan | `Warn=True`, TANPA damage, trap tetap armed (jendela belajar) |
| F3 | Disarm | `TryDisarm=True` → tidak armed |
| F4 | Reset | Kembali armed (siap untuk run berikutnya) |

## G. Console & visual

| # | Cek | Kriteria PASS |
|---|---|---|
| G1 | Console sesi | 0 error; 0 `MissingComponentException`/`ArgumentException`/`NullReferenceException` |
| G2 | Warning baru | Tidak ada warning selain yang sudah didokumentasikan (fallback `2347`, tabrakan nama assembly) |
| G3 | Screenshot game view | Render benar (geometry, props, HUD terbaca) |

## H. Sengaja tidak di smoke ini (catat sebagai NOT_RUN beralasan)

| # | Item | Alasan |
|---|---|---|
| H1 | Tulis save ke disk | Cegah polusi `persistentDataPath`; milik `SaveRepositoryTests` |
| H2 | Death → reload penuh | Jalur risiko tinggi; butuh skenario khusus + sign-off |
| H3 | Input fisik tombol-per-tombol | Milik playtest manusia |
| H4 | Final encounter/Adrian | 0 implementasi; eskalasi §8 |
