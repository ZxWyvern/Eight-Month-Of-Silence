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

### Fase 1 — Fondasi Bot (selesai)
- Main Menu production slice di-commit (`e7bf420`).
- GameLifetimeScope dengan menu-only registration.
- ADR-0027 accepted; ADR-0028 dibuat dan belum dipush.

### Fase 2 — Siapkan Tkp1 sebagai Kanvas Bab 1
1. Buka `Assets/Scenes/Tkp1.unity` (build index 2).
2. Pindahkan/pasang pemain, kamera, GameLifetimeScope, dan UI dari `PunyaPandu` ke `Tkp1`.
3. Set root `Tkp1` ke `(0,0,0)` agar floor dan prop rata.
4. Pasang `ChapterEntryPresenter` agar `M01_Alley` otomatis dimulai saat memasak scene.
5. Pasang `ChapterCompletionPresenter` untuk menampilkan kartu selesai dan kembali ke Main Menu.

### Fase 3 — Gameplay & Interaksi B1
1. Pasang 3 evidence B1 (`B1_ItemBody`, `B1_ItemBlood`, `B1_ItemScratch`) sebagai `WorldInspectableItem` di lokasi yang sesuai.
2. Pasang `B1_SavePoint` sebagai `ChapterSavePoint` dengan `pointId = b1_entry`, `fireOnce = true`.
3. Pasang `B1_PrefabBody/Blood/Scratch` sebagai inspection prefab.
4. Pasang `ReconstructionBoard` dengan case `case_b1`, opsi benar `opt_panic`.
5. Pasang `EvidenceLog`, `MissionHud`, `InspectionRig`, `InspectionUI`, `TitleCardView`, `SubtitleView`.

### Fase 4 — Sistem & Flow
1. `ChapterSavePoint` tulis checkpoint bab saat pemain masuk (sudah diimplementasi).
2. `DeathReloadPresenter` fail/retry dengan scene guard (sudah diimplementasi).
3. `PauseMenuPresenter` fallback ke chapter save dan scene guard (sudah diimplementasi).
4. `Act1ObjectiveChain` berhenti di bab boundary, publish `Act1ChapterCompletedEvent` (sudah diimplementasi).
5. `InspectionSessionService.PeekFirstValidHotspot` untuk hint inspeksi (sudah diimplementasi).
6. `ItemInspectionUI` hint text dan `ItemInspectionController` refresh hint setiap 0.15 detik (sudah diimplementasi).

### Fase 5 — Tutorial Dialogue
1. Tambah `DialogueTrigger` untuk: movimento, interaksi, inspeksi, Evidence Log/inventory, reconstruction, dan pesan penutup James.
2. Gunakan `SAMPLE_Speaker_James`, `SAMPLE_Line_Observe`, `SAMPLE_Line_Note` sebagai awal; buat line baru jika perlu.
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

1. Commit dan push `docs/plans/mvp-chapter1.md`.
2. Siapkan `Tkp1` dengan pemain, kamera, dan UI (Fase 2).
3. Pasang evidence B1 dan save point (Fase 3).
4. Play-mode verification semua alur (Fase 6).
5. Jika verifikasi berhasil, update ADR-0028 ke `Accepted` dan promote `Tkp1` ke build index 2.
