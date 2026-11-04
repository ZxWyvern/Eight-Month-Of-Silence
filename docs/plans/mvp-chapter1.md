# Plan MVP — Bab 1 (TKP)

**Status:** Draft  
**Dokumen:** `docs/plans/mvp-chapter1.md`  
**Sumber:** `docs/10-production-scope.md`, `docs/11-risks-and-dod.md`, `docs/05-chapter-structure.md`, `docs/07-ui-ux.md`

---

## 1. Tujuan

Implementasikan Minimum Viable Product (MVP) Bab 1 sebagai level TKP yang dapat diselesaikan dari awal hingga akhir, memulai melalui Main Menu, berakhir di komentar bab dan kembali ke Main Menu. Tidak ada segmen rumah Adrian, tidak ada multiplayer, tidak ada dunia terbuka.

---

## 2. Cakupan MVP (sesuai `docs/10-production-scope.md`)

| Fitur | Detail MVP |
|-------|------------|
| Level | 1 level crime scene yang dapat diselesaikan (TKP / Bab 1) |
| Movement | Walk, sprint, crouch, jump terbatas, stamina, surface footsteps |
| Evidence | 3 evidence, rotate & zoom, hotspot, Evidence Log |
| Inventory | 2 slot tangan + 4 slot kantong |
| Puzzle | 1 reconstruction puzzle |
| Objectives | 1 objective chain: masuk lokasi → observasi → deduksi |
| Sistem | Checkpoint dasar, pause, settings minimum, fail/retry flow |
| UI | Main Menu, Pause Menu, HUD Mission, Evidence Log, Inspection UI, Chapter Completion |

---

## 3. Fase Implementasi

### Fase 1 — Fondasi Bot (selesai, committed + pushed)
- Main Menu production slice di-commit (`e7bf420`).
- GameLifetimeScope dengan menu-only registration.
- ADR-0027 accepted; ADR-0028 Proposed, committed + pushed (`eb5d4c5`, `b71b726`).
- `ChapterEntryPresenter` + `ChapterCompletionPresenter` baru; guard checkpoint/scene, Act1 chapter boundary, inspection peek/hint — code committed, **integrasi scene + verifikasi runtime masih pending**.

### Fase 2 — Siapkan Tkp1 sebagai Kanvas Bab 1 (assembled 2026-11-02, verifikasi parsial PASS)

> Hasil sesi 2026-11-02: rig gameplay dirakit di `Tkp1` via salinan prefab (PunyaPandu TIDAK diubah/dihancurkan).
> Pengukuran: root `GameObject` di `(30.69, -0.04, -16.59)` — offset terbukti milik environment,
> bukan penyebab misalignment; tidak ada zero-buta. Titik spawn `(0, 1, -30)` raycast aman di atas Cube koridor.

1. Buka `Assets/Scenes/Tkp1.unity` (build index 2) — DONE, scene tersimpan via MCP.
2. Ukur transform lokal/dunia root `GameObject` + anaknya — DONE (`GameObject/Cube (10)`:
   dunia `(-8.46, 0.01, 22.17)` vs lokal `(-39.16, 0.05, 38.77)`, skala `(21.28, 0.10, 23.24)`;
   raycast koridor `x=-6..6, z=-34..-28` hit `y=0.08` aman; `z=-25..-22` kosong).
3. Ratakan posisi hanya bila terbukti perlu — TIDAK DILAKUKAN (tidak terbukti perlu; plan revisi `9fe70ea` dipush).
4. Pasang `ChapterEntryPresenter` (`ChapterEntry`) + `ChapterCompletionPresenter` (`ChapterCompletion`) —
   DONE, keduanya di `presenterRoots` GameScope (total 19: 14 rig + HealthHud + WaterZone_Demo + 2 chapter + B1_SavePoint).
5. `GameScope.playerRoot` → `YumiwaPlayer`; `PauseMenu` (player/movement/footsteps/subtitles) +
   `ReconstructionBoard.playerTransform` + `InspectionRig.ui` + `PlayerInteractor.inspection` di-wire ulang
   (referensi prefab-copy yang putus) — DONE.
6. Bukti B1 di TKP: `B1_Evidence_Body (-3, 0.5, -32)`, `B1_Evidence_Scratch (-1, 0.5, -33)`,
   `B1_Evidence_Blood (1.5, 0.5, -31.5)` — semua `itemId` + `inspectionPrefab` utuh;
   `B1_SavePoint (0, 1, -28)` `pointId=b1_entry, fireOnce=true` — DONE.
7. Play-verify parsial: run-1 `PlayerInteractor has no inspection controller` (ERROR, diperbaiki);
   run-2 console 0 error, `Mission: M01_Alley (Chapter1_BloodTrail)` via ChapterEntry, container ready,
   screenshot `Assets/Screenshots/tkp1-phase2-rig-play.png` — PASS parsial.
   Sisa: jalan-ke-evidence, inspect/take, rekonstruksi solve, death/pause/retry, kembali Main Menu (Fase 6).

### Fase 3 — Gameplay & Interaksi B1 (paket prefab Chapter1, siap drop ke scene real — 2026-11-04)

> Dua prefab kit komposisi (nested prefab, bukan re-snapshot scene):
> - `Assets/_Yumiwa/Prefabs/Chapter1/B1_OfficeKit.prefab` — alur Office: `B1_Door_ToTkp1`
>   (`DoorScenePortal`, `targetSceneName = Tkp1`) + `B1_CallTrigger` (BoxCollider trigger +
>   `DialogueTrigger` mode Sequence `seq_inspector_call` = `Call_Seq_InspectorCall`: L1–L4/J1–J3).
> - `Assets/_Yumiwa/Prefabs/Chapter1/B1_Tkp1Kit.prefab` — full rig + konten B1 (19 anak):
>   `GameScope` (presenterRoots 19, playerRoot → YumiwaPlayer), `YumiwaPlayer` (Tkp1, spawn `(0,1,-30)`),
>   `InspectionRig`+`InspectionUI`, `StaminaHud`, `InventoryHUD`, `EvidenceLog`, `MissionHUD`,
>   `SubtitleView`, `TitleCardView`, `ReconstructionBoard` (`case_b1`), `PauseMenu`, `EventSystem`,
>   `B1_Evidence_Body/Scratch/Blood`, `B1_SavePoint` (`b1_entry`), `ChapterEntry`, `ChapterCompletion`.
> Cara pakai: drag `B1_OfficeKit` ke scene Office (atur posisi pintu/trigger mengikuti layout),
> drag `B1_Tkp1Kit` ke scene TKP1 (atur posisi 3 evidence + save point mengikuti layout nyata).

### Fase 3 (asal) — Gameplay & Interaksi B1
1. Pasang 3 evidence B1 (`B1_ItemBody`, `B1_ItemBlood`, `B1_ItemScratch`) sebagai `WorldInspectableItem` di lokasi yang sesuai.
2. Pasang `B1_SavePoint` sebagai `ChapterSavePoint` dengan `pointId = b1_entry`, `fireOnce = true`.
3. Pasang `B1_PrefabBody/Blood/Scratch` sebagai inspection prefab.
4. Pasang `ReconstructionBoard` dengan case `case_b1`, opsi benar `opt_panic`.
5. Pasang `EvidenceLog`, `MissionHud`, `InspectionRig`, `InspectionUI`, `TitleCardView`, `SubtitleView`.

### Fase 4 — Sistem & Flow (code committed; integrasi + verifikasi pending)
1. `ChapterSavePoint` tulis checkpoint bab saat pemain masuk.
2. `DeathReloadPresenter` fail/retry dengan scene guard.
3. `PauseMenuPresenter` fallback ke chapter save dan scene guard.
4. `Act1ObjectiveChain` berhenti di bab boundary, publish `Act1ChapterCompletedEvent`.
5. `InspectionSessionService.PeekFirstValidHotspot` untuk hint inspeksi.
6. `ItemInspectionUI` hint text dan `ItemInspectionController` refresh hint setiap 0.15 detik.

### Fase 5 — Tutorial Dialogue (pending)
1. Tambah `DialogueTrigger` B1: movement, interaksi, inspeksi, Evidence Log/inventory, reconstruction, dan pesan penutup James.
2. Gunakan SAMPLE hanya sebagai referensi; buat aset B1 (`B1_Line_*`, `B1_Speaker_James`, `B1_Sequence_*`) — jangan ship teks SAMPLE sebagai final.
3. Dialogue non-blocking, tidak gate progresi.

### Fase 6 — Polish & Verifikasi
1. `FootstepPresenter`: matikan debug surface subtitle secara default.
2. `EvidenceLogPresenter`: kelompokkan ke Facts / Leads / Unresolved (pakai `GetFacts()`, `GetLeads()`, `GetUnresolved()`).
3. Play-mode verification: berjalan ke evidence, inspect, take, rekonstruksi, gagal/mati, pause, settings, kembali ke Main Menu.
4. Build smoke test `StandaloneWindows64`.
5. Update ADR-0028 dari `Proposed` menjadi `Accepted`.

---

## 4. Artefak yang Sudah Dibuat / Diubah

| File | Status | Keterangan |
|------|--------|------------|
| `docs/plans/mvp-chapter1.md` | Baru | Rencana MVP Bab 1 |
| `docs/architecture/adr-0028-tkp1-chapter1-boot.md` | Baru | ADR untuk Tkp1 Chapter 1 boot |
| `docs/DECISIONS.md` | Diubah | ADR-0028 ditambahkan |
| `Assets/_Yumiwa/Presentation/ChapterEntryPresenter.cs` | Baru | Inisialisasi mission flow |
| `Assets/_Yumiwa/Presentation/ChapterCompletionPresenter.cs` | Baru | Kartu selesai bab + kembali Main Menu |
| `Assets/_Yumiwa/Presentation/ChapterSavePoint.cs` | Diubah | Fire-once, guard duplicate save |
| `Assets/_Yumiwa/Presentation/DeathReloadPresenter.cs` | Diubah | Scene guard pada cold resume |
| `Assets/_Yumiwa/Presentation/MissionHudPresenter.cs` | Diubah | Hapus auto-start |
| `Assets/_Yumiwa/Presentation/PauseMenuPresenter.cs` | Diubah | Fallback chapter save + scene guard |
| `Assets/_Yumiwa/Presentation/InspectionHotspotPresenter.cs` | Diubah | `TryPeekValid` |
| `Assets/_Yumiwa/Presentation/ItemInspectionUI.cs` | Diubah | Hint text |
| `Assets/_Yumiwa/Presentation/ItemInspectionController.Flow.cs` | Diubah | Refresh hint periodik |
| `Assets/_Yumiwa/Application/Act1ObjectiveChain.cs` | Diubah | Hapus `mission.Advance()` |
| `Assets/_Yumiwa/Application/Tests/Act1ObjectiveChainTests.cs` | Diubah | Assert tetap di `M01_Alley` |
| `Assets/_Yumiwa/Application/InspectionSessionService.cs` | Diubah | `PeekFirstValidHotspot` |

---

## 5. Definition of Done (sesuai `docs/11-risks-and-dod.md`)

- [ ] Pemain memahami objective tanpa waypoint berlebihan
- [ ] Pemain menemukan bukti lewat observasi, bukan semua objek menyala
- [ ] Inspection menghasilkan informasi yang memengaruhi progres
- [ ] Pemain memahami perbedaan item tangan / kantong / Evidence Log
- [ ] Transisi investigasi → survival terasa eskalasi, bukan ganti genre
- [ ] Puzzle dapat diselesaikan dari clue yang tersedia
- [ ] Frame rate target tercapai pada vertical slice (saat dirilis)
- [ ] Ending menyelesaikan kasus tapi meninggalkan konsekuensi emosional
- [ ] Kode compile tanpa error / warning
- [ ] Play-mode verification tanpa `MissingComponentException`, `ArgumentException`, atau `NullReferenceException`

---

## 6. Risiko & Mitigasi (sesuai `docs/11-risks-and-dod.md`)

| Risiko | Mitigasi |
|--------|----------|
| Inspection jadi gimmick | Setiap hotspot menghasilkan fakta, tool usage, atau consequence |
| Inventory terbatas → soft-lock | Critical evidence masuk log; objective item punya recovery rule |
| Movement terasa lambat | Respons input cepat; berat dari animasi & audio, bukan input lag |
| Narasi terlalu banyak teks | Prioritaskan clue visual dan interaksi |
| Scope terlalu besar | Validasi Bab 1 sebelum full production |

---

## 7. Langkah Berikutnya

1. Commit dan push revisi `docs/plans/mvp-chapter1.md`.
2. Siapkan `Tkp1` dengan pemain, kamera, dan UI (Fase 2).
3. Pasang evidence B1 dan save point (Fase 3).
4. Play-mode verification semua alur (Fase 6).
5. Jika verifikasi berhasil, update ADR-0028 ke `Accepted` dan promote `Tkp1` ke build index 1 (`PunyaPandu` ke index 2).
