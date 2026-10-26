# Workflow agent: cerita dan mekanik Eight Months of Silence

Baseline: 27 September 2026. Dokumen ini menghubungkan GDD, kode yang sudah ada, dan tiket pekerjaan berikutnya. Jalankan setiap tiket melalui sembilan fase di [workflow.json](workflow.json), dengan profil [workflow-agent](../agents/workflow-agent.md). Ini backlog implementasi, bukan pernyataan bahwa seluruh fitur sudah selesai atau izin menjalankan semua tiket sekaligus.

## 1. Dasar dan batas bukti

Sumber cerita: [struktur bab](../docs/05-chapter-structure.md), [karakter](../docs/04-narrative-and-characters.md), dan [level](../docs/06-level-design.md). Scope produksi mengikuti [MVP dan vertical slice](../docs/10-production-scope.md); keputusan terbuka mengikuti [daftar TBD](../docs/12-references-and-open-tbd.md). Kebutuhan mekanik dan UI mengikuti [core gameplay](../docs/03-core-gameplay-systems.md) dan [UI/UX](../docs/07-ui-ux.md).

Unity MCP pada sesi authoring mengonfirmasi project `CeBeTe`, Unity `6000.3.8f1`, URP, dan scene aktif `Assets/Scenes/PunyaPandu.unity` pada port `7891`. Port harus ditemukan ulang pada sesi berikutnya. Scene prototipe ini bukan bukti Bab 1 telah selesai.

Kode checkpoint, inspection, inventory presenter, world pickup, death reload, mission flow, dan puzzle/combat dibaca melalui MCP pada sesi ini. Peta sistem lain mengacu pada review sebelumnya dan [code-map](code-map.md); verifikasi ulang pemilik serta wiring sebelum implementasi. Peta lama bertanggal 23 September masih menyebut snapshot ekspor dan save v3; kode sekarang sudah memuat field snapshot v4. Jangan mengulang temuan lama sebagai bug yang pasti masih ada.

Tidak ada Play+Execute baru dalam authoring dokumen ini. Semua tiket di bawah mulai dengan hasil verifikasi `NOT_RUN`. Istilah "fondasi ada" berarti kode ditemukan, bukan fitur sudah terintegrasi atau diterima sebagai `DONE`.

## 2. Mekanik saat ini dan pekerjaan yang tersisa

Path di kolom kode relatif terhadap `Assets/_Yumiwa/`. Untuk akar snapshot ekspor, ikuti deteksi root di profil agent.

| Mekanik | Fondasi yang ada | Yang harus diselesaikan/dibuktikan |
|---|---|---|
| Movement dan stamina | `Application/MovementStateMachine.cs`, `Domain/PlayerMovementState.cs`, `Presentation/PlayerMovementPresenter*.cs` | Walk/sprint/crouch/limited jump di level cerita, surface footsteps, tuning dan aksesibilitas; jangan tulis ulang motor |
| Interaksi dan inspection | `Presentation/PlayerInteractor.cs`, `ItemInspectionController*.cs`, `Application/InspectionSessionService.cs` | Tiga evidence Bab 1 dengan rotate/zoom/hotspot, petunjuk yang terbaca, Cancel dan input kembali normal |
| Inventory fisik | `Application/PhysicalInventoryService.cs`, `Presentation/InventoryPresenter.cs`, grid/HUD/equipment presenters | Dua tangan + empat pocket, penuh/swap/drop/recovery, input UI tidak memakai item atau menembakkan pistol |
| Evidence Log | `Application/EvidenceLogService.cs`, `Domain/Evidence.cs` | Bukti cerita nyata dan hubungan fact/dependency/consequence; rollback konsisten dengan hotspot |
| Puzzle | `Application/PuzzleService.cs`, `Presentation/PuzzleView*.cs` | Implementasi saat ini memeriksa prerequisite dan `SolutionCode`; rekonstruksi kasus lewat bukti belum terbukti hanya dari service ini |
| Objective dan mission | `Application/ObjectiveService.cs`, `MissionFlowService.cs` | Syarat berbasis bukti/deduksi, transisi sekali saja, urutan cerita dan resume; indeks mission bukan chapter loader |
| Save dan retry | `Application/CheckpointService.cs`, `Domain/CheckpointData.cs`, `Infrastructure/JsonFileSaveRepository.cs`, `Presentation/DeathReloadPresenter.cs` | Konsistensi dunia/inventory/hotspot/evidence, chapter save vs house milestone, restart aplikasi; tiga tiket awal mengatasi risiko progres/input |
| Trap, health, flashlight | `Application/TrapService.cs`, `HealthService.cs`, `FlashlightService.cs`, `Presentation/TrapZone.cs` | Wiring satu segmen rumah dengan warning terbaca, disarm/keluar zona, damage/retry dan puzzle lingkungan |
| Combat | `Application/CombatService.cs`, `Domain/WeaponState.cs` | Service yang dibaca hanya ammo/fire/reload dan event; hit target, ancaman, melee dan final encounter belum terbukti |
| Kamera dokumentasi, rekonstruksi, wawancara, radio, perpindahan chapter | Belum ditemukan implementasi lengkap pada review sumber sebelumnya | Cari ulang sebelum menambah sistem; paket Timeline/Addressables terpasang tidak berarti rangkaian cerita sudah dibuat |
| Pause/settings dan presentasi audio | Ada fondasi config/feedback pada review sebelumnya | Menu lengkap, persistensi setting, kontrol yang pulih, surface footsteps, subtitle/fallback dan uji integrasi |

## 3. Urutan cerita yang harus dipertahankan

| Bab | Aksi pemain → informasi → konsekuensi | Mekanik utama dan keluaran |
|---|---|---|
| 1. Jejak Darah Pertama | Periksa tubuh, goresan, blood trail dan tanda perlawanan → pelaku kehilangan kendali dan kabur terburu-buru → James melihat awal rangkaian pembunuhan | Tutorial movement/interaksi/inspection/kamera, Evidence Log, satu rekonstruksi dan objective chain di gang; 26 Mei pukul 23.45, tahun TBD-1 |
| 2. Pembunuhan di Motel | Periksa dua korban Kamar 201 dan barang yang hilang/tertinggal, akses CCTV → pria tinggi berjaket hitam memakai mobil tua tanpa pelat → motif perampokan diragukan | Investigasi multi-room, puzzle pemulihan/akses CCTV, rekonstruksi urutan kejadian |
| 3. Saksi yang Selamat | Wawancarai Arudt dan bandingkan kesaksian dengan bukti → ciri wajah pelaku → sketsa tersebar, pelaku menghilang delapan bulan | Percakapan berbasis teks, perbandingan fakta, penyusunan facial description; 15 Oktober, tahun TBD-1 |
| 4. Wajah di Tengah Kabut | Ikuti informasi Ray → pembuntutan dari pom bensin ke Hutan Rute 9 dan putusnya radio → transisi menuju manhunt | Rangkaian radio/teks dan perjalanan malam; Ray ditembak sebelum James tiba, bukan cabang penyelamatan Ray |
| 5. Rumah Tanpa Jalan Keluar | Temukan Ray di mobil patroli → amati dan lewati jebakan rumah → akses basement dan hadapi Adrian | Inventory, trap, puzzle, resource pressure, final confrontation; Adrian dilumpuhkan, penyelesaian kasus disusul dampak emosional kehilangan Ray |

Urutan lokasi: gang → motel → jalan sepi → pom bensin → Hutan Rute 9 → rumah kayu → basement. Jangan menyimpulkan adanya driving gameplay dari perjalanan malam, menambahkan lokasi, atau mengarang kondisi akhir Adrian. Detail ending dan combat final tetap TBD-3/TBD-5. Gunakan nama James sampai perbedaan penulisan nama belakang di sumber diselesaikan.

## 4. Cara agent memilih dan menjalankan pekerjaan

1. Baca kontrak dan katalog skill/MCP sesuai urutan wajib. Invokasi `help` atau `project-stage-detect` pada INTAKE. Baca dokumen ini setelah discovery, lalu pilih satu tiket sesuai permintaan pengguna. Permintaan membuat workflow tidak mengotorisasi implementasi game.
2. Jika pengguna meminta pekerjaan berikutnya tanpa ID, pilih tiket paling awal yang relevan dan dependensinya sudah punya bukti PASS. Default produksi: bug penghambat → MVP Bab 1 → vertical slice Bab 1 + segmen rumah → ekspansi Bab 2 sampai 5. Segmen rumah pada slice adalah sampel produksi; urutan cerita rilis tetap lima bab.
3. Salin konteks tiket ke [task-template](task-template.md). Untuk beat cerita, isi [narrative-beat-template](narrative-beat-template.md): aksi, informasi, konsekuensi, prasyarat, kontrol, cancellation, repeat, persistence. Tiket yang terlalu besar boleh dibagi menurut hasil yang dapat dimainkan; acceptance induknya tetap berlaku.
4. Verifikasi source dan scene saat ini. Jika fitur ternyata sudah ada, lakukan verifikasi/integrasi yang kurang. Jika bug tidak dapat direproduksi dan sumber sudah berubah, catat buktinya dan ubah backlog; jangan membuat fix spekulatif.
5. Jalankan sembilan fase tanpa menghapus gate. Reuse service, DI, config dan input yang ada. Pemilik aturan tetap Domain/Application; Presentation memegang input/scene/UI; Infrastructure memegang I/O/loading. Jangan menambah manager global, graph editor, framework dialog, atau migrasi namespace demi satu beat.
6. Pilih tes di [verification-matrix](verification-matrix.md), tambah tes perilaku hanya pada celah yang relevan. Setiap implementasi runtime wajib diuji lewat pemain/UI sebenarnya, compiler, console, state/screenshot, lalu stop Play. Test service saja tidak membuktikan input/scene bekerja.
7. Catat hasil per acceptance dan dependensi. Bukti PASS dari tiket yang di-HANDOFF boleh menjadi masukan teknis tiket berikutnya dalam scope yang sudah diotorisasi; hanya manusia menetapkan DONE. Kemajuan stage produksi tetap memakai `gate-check`. Jangan menjalankan seluruh backlog atau meminta persetujuan ulang untuk setiap langkah rutin.

Jika skill/MCP wajib tidak tersedia, tahan fase yang terblokir, dokumentasikan operasi yang belum bisa dilakukan, dan kerjakan persiapan independen yang diotorisasi. Membaca SKILL.md tidak sama dengan menjalankan skill. Dokumen ini tidak melemahkan aturan tersebut.

## 5. Tiket prioritas: progres dan input

Semua skenario berikut harus direproduksi ulang sebelum fix. Jalur sumber saat authoring masih menunjukkan risikonya; hasil runtime review sebelumnya tidak dihitung sebagai PASS untuk perubahan baru.

| ID | Dependensi | Scope dan pemilik awal | Acceptance dan regresi wajib |
|---|---|---|---|
| FIX-01 | Tidak ada | P1: pickup objective hilang setelah rollback. Trace `CheckpointService.LoadCheckpoint` → inventory import; `WorldInspectableItem.Consume/Restore`; `DeathReloadPresenter`; `AutoReturnService` | Save sebelum Take, ambil key, mati/reload: key kembali tersedia sesuai snapshot. Save setelah Take: key tetap dimiliki tanpa duplikat di dunia. Ulangi drop/recovery, inventory penuh, reload dua kali dan restart aplikasi. Jangan restore semua pickup tanpa memeriksa ownership |
| FIX-02 | FIX-01 | P2: hotspot sudah ditemukan tetapi Evidence Log kembali ke snapshot lama. Trace `InspectionSessionService.ImportDiscoveredKeys`, `ExamineHotspot` dan checkpoint import | Save kosong → examine → Cancel → reload: hotspot dan evidence sama-sama mengikuti snapshot dan bisa ditemukan lagi. Save setelah examine: discovery/evidence tetap ada sekali. Uji snapshot kosong/null sesuai kompatibilitas versi; Cancel tanpa reload tetap mempertahankan observasi |
| FIX-03 | Tidak ada | P2: inventory terbuka masih menerima use/fire. Trace `InventoryPresenter.OnUseHand/SetInventoryOpen`, input adapter dan action asset aktif | Dengan pistol di tangan, klik dan pindahkan slot: ammo tidak berkurang dan tidak ada ItemUsed. Close: gameplay kembali normal. Uji mouse/keyboard, Escape, inspection, puzzle, pause saat tersedia, disable/re-enable. Tetapkan kontrol modal tanpa mematikan tombol close atau event UI |

FIX-01 dan FIX-02 mengubah pemulihan state, bukan izin mengganti format save. Jika solusi membutuhkan field/schema baru, siapkan dampak kompatibilitas dan keputusan sesuai AGENTS.md §8 sebelum implementasi. Tes repository asli diperlukan untuk persistensi; fake in-memory tidak cukup.

## 6. MVP Bab 1

| ID | Dependensi | Hasil yang dikerjakan | Reuse dan verifikasi |
|---|---|---|---|
| B1-01 | FIX-01, FIX-02, FIX-03 | Tiga evidence authored di gang, tutorial interaksi/rotate/zoom/hotspot dan Evidence Log. Pilihan objek harus menunjang tubuh/goresan/blood trail/perlawanan pada kanon; jangan mengarang fakta kasus | Reuse WorldInspectableItem, inspection/config/log dan inventory. Tiap bukti punya ID stabil, fact/dependency/consequence. Mainkan ketiganya; cek Take/Cancel/penuh/ulang/reload dan bukti penting tidak memakan slot fisik |
| B1-02 | B1-01 | Interaksi kamera dokumentasi sesuai GDD Bab 1 | Cari implementasi yang ada dahulu. Tetapkan syarat foto dan hasil observasi yang dibutuhkan beat; gunakan data bukti yang ada. Foto gagal/salah tidak membuka progres, foto benar tidak menggandakan bukti, Cancel memulihkan kontrol. Penyimpanan gambar/galeri bebas bukan kebutuhan otomatis; keputusan persistence baru mengikuti gate save |
| B1-03 | B1-01 | Satu rekonstruksi: pemain menghubungkan bukti menjadi kesimpulan pelaku kehilangan kendali dan kabur terburu-buru | Evaluasi batas PuzzleService yang saat ini menerima SolutionCode. Buat representasi paling kecil yang mendukung aksi deduksi; jangan sekadar mengganti label keypad. Kurang bukti ditolak dengan alasan, jawaban salah bisa diulang tanpa kehilangan progres, benar membuka objective tepat sekali, reload konsisten |
| B1-04 | B1-02, B1-03 | Hubungkan masuk TKP → observasi → deduksi → selesai Bab 1; chapter save dan resume | Reuse ObjectiveService, MissionFlowService, CheckpointService dan presenter. Belum deduksi tidak boleh advance hanya karena trigger dilewati. Kembali ke trigger/reload tidak melewati dua tahap. Uji cold start, resume, fail/retry, dan jangan memakai house milestone sebagai save Bab 1 |
| MVP-01 | FIX-03 | Pause/resume, settings minimum dan surface footsteps untuk level MVP | Audit MovementPresenter/config/input dan UI/audio yang ada. Pause menyediakan Resume, Load Checkpoint, Settings dan Main Menu. Cocokkan docs/07-ui-ux.md: graphics, audio, sensitivity, FOV, head-bob/shake, subtitle default aktif dan key rebinding. Terapkan opsi yang dipersyaratkan atau catat blocker, jangan diam-diam membuangnya. Uji persistensi settings, reset, konflik modal, permukaan footstep dan cue yang tetap dipahami tanpa audio |

Gate MVP, setelah B1-04 dan MVP-01: satu level crime scene dapat diselesaikan dari awal; tiga evidence; rotate/zoom/hotspot/log; dua hand + empat pocket; satu rekonstruksi; satu objective chain; walk/sprint/crouch/limited jump/stamina/surface footsteps; checkpoint dasar, pause/settings dan fail/retry. Semua harus punya bukti aktual. Fondasi kode tidak memenuhi gate ini sendiri. Jalankan `gate-check` ketika mengusulkan perpindahan stage.

## 7. Vertical slice

| ID | Dependensi | Hasil yang dikerjakan | Reuse dan verifikasi |
|---|---|---|---|
| VS-01 | B1-04, MVP-01, gate MVP PASS | Satu segmen rumah Adrian dengan satu trap dan satu environmental puzzle yang saling mendukung | Reuse TrapService/TrapZone, PuzzleService, inventory, health/flashlight dan recovery. Warning terlihat sebelum damage; keluar/disarm membatalkan trigger; tools wajib tetap dapat diakses. Mainkan pilihan loadout, inventory penuh, drop item wajib, gagal puzzle, mati/reload dan akses rute setelah solve |
| VS-02 | VS-01 | Satu encounter situasional yang membuktikan tekanan survival | Mulai CLARIFY dengan ancaman, aksi pemain, outcome, retry dan kebutuhan hit/damage yang konkret. CombatService ammo bukan AI musuh. Jika Adrian/final state machine tersentuh, keputusan manusia wajib sebelum edit; TBD-5/TBD-6 tetap terbuka. Implementasi dibatasi encounter yang disetujui, tanpa combat berulang atau AI framework umum |

Gate slice: Bab 1 lengkap + segmen rumah; satu trap, puzzle lingkungan, encounter situasional; art/lighting/ambience/UI representatif; bukti Play+Execute, Windows build dan profiling pada hardware yang dicatat. Target durasi 15–25 menit diukur melalui playthrough, bukan diklaim dari perkiraan. Target hardware TBD-7 dan angka final TBD-6 tidak diputuskan agent. Gate tidak PASS selama VS-02 atau pengukuran wajib masih terblokir. Transisi demo ke segmen rumah diberi konteks slice dan tidak menjadi urutan cerita final.

## 8. Ekspansi cerita setelah slice

Tiket ini direncanakan sebagai pekerjaan pasca-slice. Eksekusi tetap mengikuti scope yang diotorisasi; perencanaan tidak menetapkan detail canon atau arsitektur baru.

| ID | Dependensi | Beat dan mekanik yang belum lengkap | Acceptance utama |
|---|---|---|---|
| STORY-01 | VS-02, gate slice PASS | Hubungkan akhir Bab 1 ke Bab 2 dengan loading dan resume yang benar | Reuse mission/save dan periksa loader aktif. Pilihan API mengikuti kontrak/ADR dan context7, bukan dugaan dari paket terpasang. Scene target siap sebelum kontrol dilepas; kegagalan/cancel loading tidak kehilangan save/progres atau menggandakan pemain. Tetapkan persistence lintas bab sebelum menambah save field |
| B2-01 | STORY-01 | Motel Kamar 201: multi-room evidence, akses/pemulihan CCTV dan rekonstruksi kejadian | CCTV terkunci sampai prasyarat terpenuhi, output visual/teks mendukung ciri pelaku/mobil kanon. Pemain menemukan kontradiksi motif perampokan melalui aksi. Jawaban salah bisa diulang; bukti, solve dan chapter transition bertahan saat resume |
| B3-01 | B2-01 | Wawancara teks Arudt, perbandingan evidence dengan kesaksian dan facial description | Jawaban ulang tidak menduplikasi fakta; mata hitam, luka dekat mata kiri dan jahitan bibir konsisten. Sketsa dan transisi delapan bulan dipicu setelah syarat terpenuhi. Tetapkan kontrak save Bab 3 sebelum edit persistence; jangan otomatis menyalin house checkpoint |
| B4-01 | B3-01 | Urutan informasi Ray, pom bensin, Rute 9, radio terputus dan kedatangan James | Isi narrative-beat-template untuk siapa melihat/mendengar tiap peristiwa. Cue terurut, subtitle/fallback tersedia; pause/death/unload/re-entry tidak mengulang konsekuensi atau mengunci input. Ray tetap tewas sebelum James tiba. Tidak menambah driving/combat atau cabang penyelamatan; tetapkan resume Bab 4 |
| B5-01 | B4-01, VS-01 | Lengkapi rumah dan jalur basement memakai mekanik survival yang sudah dibuktikan | Rangkaikan observasi trap/puzzle dengan milestone `DisplayCaseKey`, `Shovel`, `BasementAccess` sesuai GDD/save contract. Uji rute terhalang, tool wajib hilang/drop, resource rendah, mati di tiap milestone dan restart. Cue Adrian berbasis teks/audio sesuai scope, tanpa menganggap full VO tersedia |
| B5-02 | B5-01, VS-02 | Final confrontation dan aftermath | BLOCKED untuk implementasi sampai keputusan TBD-3/TBD-5 dan izin menyentuh state machine Adrian tersedia. Siapkan opsi konkret, pakai architecture-decision untuk keputusan arsitektur yang diperlukan. Setelah disetujui: uji aturan menang/kalah/retry, resource, interruption dan resolusi Adrian dilumpuhkan tanpa menciptakan ending baru |

Gate cerita penuh: playthrough Bab 1 → 5 dan restart pada save yang didukung; tidak ada scene order salah, bukti wajib hilang, duplikasi konsekuensi, kontrol terkunci, atau jalan buntu. Verifikasi build Windows, audio/subtitle, settings, input dan performa sesuai DoD. Jangan menetapkan total durasi/harga (TBD-2), tahun (TBD-1), nama kota/polisi (TBD-4), VO (TBD-8), atau referensi TBD-9 secara sepihak.

## 9. Bukti dan pemakaian

Untuk setiap tiket, simpan: GDD/ADR rujukan; source yang dibaca saat itu; file berubah; hasil automated test yang relevan; project/port/scene; lama Play; aksi pemain; expected vs observed; compiler dan console baru; screenshot/state dump; stop Play; risiko dan pekerjaan tersisa. Catat `NOT_RUN` jika belum diuji dan `BLOCKED` jika alat/keputusan wajib tidak tersedia. Jangan mengubah hasil historis menjadi PASS baru.

Contoh pemakaian agent yang sudah ada:

```text
/eos-work Kerjakan FIX-01 dari agent/story-mechanics-workflow.md. Reproduksi dulu, lalu perbaiki scope tiket dan verifikasi melalui Unity MCP. Pertahankan save schema kecuali ada keputusan yang diperlukan.
/eos-work Audit gate MVP dari agent/story-mechanics-workflow.md secara read-only. Laporkan bukti yang tersedia dan tiket pertama yang belum memenuhi acceptance.
/eos-work Kerjakan B1-01 setelah verifikasi dependensinya. Ikuti sembilan fase, gunakan narrative-beat-template, dan batasi pada tiga evidence Bab 1.
```

Host lain: baca `AGENTS.md`, `agents/workflow-agent.md`, lalu jalankan `agent/workflow.json` untuk ID tiket tersebut. Status authoring, keterbatasan tools, dan validasi dokumen tersedia di [laporan authoring](story-workflow-report.md).
