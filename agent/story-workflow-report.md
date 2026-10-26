# Laporan authoring workflow cerita

Tanggal: 27 September 2026. Scope: `workflow_docs`. Hasil: dokumen workflow dan integrasi profil/template, tanpa perubahan kode game, scene, prefab, asset, input asset, save schema atau canon.

## Perubahan dan alasan

`story-mechanics-workflow.md` memetakan mekanik yang sudah punya fondasi, mekanik yang belum terbukti lengkap, lima bab kanon, 16 tiket, dan gate MVP/slice/cerita penuh. `workflow.json` menautkan artifact dan traceability pada route/INTAKE/PLAN. `agents/workflow-agent.md` menjelaskan pemilihan tiket; `task-template.md` menyimpan konteks cerita dan dependensi; README menyediakan titik masuk. Sembilan fase, rule IDs, permission dan kepemilikan keputusan tetap berlaku.

Dokumen ini adalah persiapan independen yang diminta pengguna. Pipeline formal tetap `BLOCKED` pada INTAKE karena runtime invokasi skill OpenCode tidak tersedia pada host authoring. Jangan menganggap pengiriman dokumen sebagai keberhasilan seluruh gate atau sebagai implementasi tiket game.

## Dampak arsitektur

Workflow/prose saja. Tidak ada dependency game, service, ADR, atau schema baru. Pembagian tiket mengikuti sistem yang sudah ada. Desain detail fitur baru ditunda sampai PLAN tiket terkait; pertanyaan ending/combat/save ditahan pada gate yang berlaku, bukan dijawab oleh backlog.

## Skills dan tools

| Capability | Penggunaan dan bukti |
|---|---|
| Katalog `.opencode/skills/*/SKILL.md` | Dibaca/diseleksi. `help` / `project-stage-detect` relevan untuk INTAKE; runtime `skill` tidak tersedia dan executable OpenCode tidak ditemukan. Tidak diklaim berhasil diinvokasi |
| `gate-check` | Diperlukan ketika stage produksi benar-benar diajukan maju; authoring ini tidak memajukan stage |
| `architecture-decision` / `create-architecture` | Tidak diinvokasi: tidak membuat keputusan arsitektur game/ADR. Tiket yang memerlukan keputusan tersebut tetap mewajibkannya |
| Skill UI global / architecture untuk project lain | Katalog diperiksa; tidak diterapkan untuk workflow teks ini atau mengganti kontrak Eight Months of Silence |
| Unity MCP `unity_list_instances`, `unity_select_instance` | Sesi baru mengonfirmasi CeBeTe pada port 7891, Unity 6000.3.8f1, bridge 2.39.5. Hasil discovery sebelumnya yang kosong tidak lagi berlaku |
| Unity MCP `unity_project_info`, `unity_scene_info` | Mengonfirmasi URP, target StandaloneWindows64, `PunyaPandu` aktif/build index 0 dan scene tidak dirty pada pemeriksaan. Build settings juga berisi scene lain; tidak diubah |
| Unity MCP `unity_script_read` | Membaca CheckpointService, CheckpointData, InspectionSessionService, InventoryPresenter, WorldInspectableItem, DeathReloadPresenter, MissionFlowService, PuzzleService, CombatService di `Assets/_Yumiwa/` |
| Transport MCP | Memakai client stdio yang sudah ada, `.opencode/mcp/unity-mcp-server/tests/helpers/mcp-client.mjs`, untuk handshake/list/call MCP. Bukan request langsung ke HTTP bridge |
| context7 | Tidak digunakan: tidak menulis pemanggilan API/library atau memutuskan API loading. Tiket implementasi tetap harus memakainya untuk API eksternal |
| Shell / apply_patch / bundled Python | Membaca dan menyunting dokumentasi serta memvalidasi struktur/referensi. `load_workspace_dependencies` menemukan Python yang dipakai validator |

Pembacaan source menunjukkan: checkpoint mengimpor inventory dan log tetapi recovery dunia bergantung pada pending IDs; hotspot import menambahkan discovery tanpa mengganti snapshot; handler use inventory tidak memeriksa modal inventory; combat yang diperiksa terbatas pada ammo/fire/reload. Ini dasar tiket reproduksi/fix, bukan bukti runtime baru bahwa semua skenario pasti gagal.

## Risiko dan batas bukti

- Belum ada pengujian runtime baru; status "fondasi ada" tidak berarti chapter dapat dimainkan sampai selesai. Area di luar source yang dibaca ulang memakai review/peta historis dan harus diperiksa saat tiket dimulai.
- Save v4 memiliki field progression; ini tidak membuktikan pemulihan dunia, kompatibilitas file, atau round-trip benar. Tidak mengubah snapshot baseline/hash historis bundle.
- Tidak mengarang implementasi CCTV, wawancara, radio atau final encounter dari komentar/source desain. Keputusan TBD dan akses tool yang belum tersedia tetap menjadi blocker pada pekerjaan terkait.
- Tidak ada perubahan hot path, concurrency atau save compatibility dalam authoring ini. Risiko operasional utamanya ialah agent berikutnya menganggap backlog sebagai fitur selesai atau izin menjalankan semuanya; guard tersebut tertulis pada profil, JSON dan dokumen utama.

## Validasi dan Definition of Done

`python agent/tools/validate_workflow.py --root . --structure-only`: `PASS`, sembilan fase, 30 rules, 16 bundle files; errors/warnings/blockers kosong. Pemeriksaan source oleh validator: `NOT_RUN` (source terpilih dibaca terpisah melalui MCP). Pemeriksaan tambahan: 16 ID tiket unik, referensi dependensi valid tanpa siklus, 16 tautan lokal valid, urutan fase/global rules/entry-exit criteria/blocked transitions sama dengan sebelum perubahan. Diff empat file existing ditinjau; dua dokumen baru ditinjau terhadap GDD. Validasi ini tidak membuktikan runtime OpenCode maupun game.

Runtime Play+Execute: `NOT_APPLICABLE` untuk perubahan dokumentasi; durasi Play, console gameplay dan screenshot gameplay baru tidak tersedia dan tidak diklaim. Eksekusi skill wajib: `BLOCKED`.

- [x] Output workflow konkret dan terhubung ke agent/template yang sudah ada.
- [x] Urutan cerita dan scope produksi bersumber dari GDD; mekanik usulan dibedakan dari fondasi kode.
- [x] Risiko, dependensi, skenario negatif/reload, canon/TBD dan batas tools tercatat.
- [x] Tidak mengubah runtime atau menetapkan keputusan desain baru.
- [x] Struktur JSON, tautan lokal, dependensi tiket dan preservasi gate tervalidasi.
- [ ] INTAKE skill `help` atau `project-stage-detect` benar-benar diinvokasi.
- [ ] Seluruh pipeline formal lolos; tidak diklaim karena blocker INTAKE.

Ledger formal: INTAKE `BLOCKED`; CLARIFY, PLAN, ARCHITECTURE_CHECK, IMPLEMENT, SELF_REVIEW, TEST, DOCUMENT dan HANDOFF `NOT_RUN` sebagai transisi formal. Authoring dan validasi dokumentasi adalah persiapan independen yang diperbolehkan jalur `on_blocked` pada workflow, bukan penggantian invokasi skill atau pelompatan fase.

## Langkah berikutnya

Jalankan invokasi INTAKE melalui host OpenCode yang menyediakan skill terkait, lalu review artifact ini dengan sembilan fase dan tetapkan acceptance manusia. Untuk implementasi game yang kemudian diotorisasi, mulai dari FIX-01, reproduksi ulang dan kerjakan perubahan terkecil beserta Play+Execute. Jangan menandai tiket DONE berdasarkan dokumen ini.
