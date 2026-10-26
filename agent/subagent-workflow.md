# Subagent untuk story dan mechanics

Gunakan `eos-workflow` sebagai koordinator dan tiga subagent berikut sesuai kebutuhan tiket. Profil ini memakai format `mode: subagent` yang sudah digunakan di repository. Tidak menetapkan model, memasang plugin, atau mengubah permission.

| Subagent | Profil peran yang dipakai | Tugas |
|---|---|---|
| `eos-story` | `agents/narrative-agent.md`, `agents/game-design-agent.md` | Memetakan aksi pemain, informasi, konsekuensi dan acceptance sesuai cerita |
| `eos-mechanics` | `agents/systems-agent.md` | Reproduksi bug, implementasi mekanik dan integrasi dalam scope tiket |
| `eos-qa` | `agents/qa-compliance-agent.md` | Review independen, skenario regresi dan pemeriksaan bukti |

## Kontrak bersama

Setiap subagent membaca `AGENTS.md` mengikuti urutan wajib, lalu `agents/workflow-agent.md`, `agent/workflow.json`, dan `agent/story-mechanics-workflow.md`. Kontrak root dan otorisasi pengguna tetap berlaku jika profil peran lama berbeda. Discovery/invokasi skill dan MCP tetap wajib; membaca file skill bukan invokasi. Tidak ada subagent yang otomatis memperoleh izin mengimplementasikan seluruh backlog.

Koordinator mengirim satu brief berisi:

```text
Ticket: FIX-01 / ID lain dari story-mechanics-workflow.md
Outcome: hasil konkret yang dibutuhkan dari subagent
Mode: read-only review / authorized implementation / authorized verification
Phase: fase workflow induk dan evidence yang sudah tersedia
Scope: file/type yang boleh diubah; file milik agent lain
Dependencies: hasil check dan lokasi evidence
Canon/TBD: rujukan, keputusan yang sudah ada, blocker yang belum selesai
Unity lease: pemilik sesi mutation/Play; project dan port hasil discovery terbaru
Return: temuan/perubahan, bukti, risiko, blocker dan next action
```

Jika brief belum lengkap, subagent boleh melengkapi konteks lewat pembacaan independen. Jangan menyimpulkan izin menulis dari permintaan review. Pertanyaan hanya diperlukan untuk keputusan material yang belum terjawab oleh scope dan kontrak.

## Pembagian kerja

1. Koordinator memilih satu tiket yang dependensinya siap. Untuk bug lokal seperti FIX-01, langsung gunakan mechanics dan QA; story tidak wajib dipanggil hanya untuk mengisi laporan.
2. Untuk tiket naratif, story menyusun kontrak beat memakai `agent/narrative-beat-template.md`. Mechanics boleh membaca kode secara paralel, tetapi menunggu kontrak yang dibutuhkan sebelum mengubah perilaku.
3. Mechanics mengerjakan perubahan terkecil pada file yang ditugaskan. Semua partial file dari tipe terkait dianggap satu kepemilikan. Koordinator memegang workflow/shared docs kecuali ditugaskan eksplisit.
4. QA meninjau diff dan skenario yang relevan setelah perubahan siap. QA tidak memperbaiki source sendiri; temuan dikembalikan ke mechanics agar tidak terjadi dua penulis.
5. Mutasi Unity, kompilasi yang memicu refresh, dan Play+Execute dijalankan bergantian. Satu pemilik mendapat akses eksklusif dari koordinator; agent lain tidak mengganti scene, input atau Play state. Discovery instance dilakukan ulang; jangan hardcode port 7891 untuk sesi mendatang.
6. Setelah pemilik menghentikan Play dan menyerahkan evidence, koordinator dapat memberikan sesi verifikasi kepada QA. QA membedakan bukti yang diperiksa dari pengujian yang benar-benar dijalankannya sendiri.
7. Koordinator menggabungkan hasil ke task/session record dan menyelesaikan sembilan fase. Output subagent bukan otomatis PASS fase induk. Jangan melewati blocker skill/MCP, perubahan save, canon, atau Adrian. Hanya manusia menetapkan DONE.

Tidak perlu menjalankan tiga agent untuk setiap tiket. Jangan membuat subagent tambahan tanpa delegasi yang diperlukan oleh tugas. Specialist Unity/UI/audio yang sudah ada tetap dapat dipakai jika scope membutuhkan keahliannya; ketiga profil ini tidak mengganti specialist tersebut.

## Bentuk hasil subagent

- ID tiket, scope, fase yang ditangani dan hasil konkret.
- File dibaca/diubah dan alur sebab-akibat temuan, dengan path/simbol yang dapat diperiksa.
- Acceptance: expected vs observed, `PASS` / `FAIL` / `BLOCKED` / `NOT_RUN` / `NOT_APPLICABLE` dan evidence.
- Skills/MCP yang benar-benar diinvokasi, tools yang tidak tersedia, serta risiko.
- Untuk runtime: project/instance/scene, lama Play, aksi, compile/console, screenshot/state dan konfirmasi stop. Jangan menyebut "sudah diuji" hanya karena membaca tes.
- Rekomendasi next action. Gunakan `HANDOFF` hanya jika scope delegasi dan gate yang berlaku terpenuhi; jika terhambat, `BLOCKED` beserta fase/alasan.

## Contoh brief ke koordinator

```text
/eos-work Gunakan eos-mechanics untuk FIX-01 dan eos-qa untuk review independen. Reproduksi sebelum fix, pertahankan schema save, satu pemilik Unity, dan kumpulkan bukti Play+Execute.
/eos-work Gunakan eos-story untuk menyiapkan kontrak beat B1-01. Read-only terhadap Assets; serahkan draft tiga evidence dan acceptance tanpa mengubah canon.
```

## Status authoring

Profil disusun dari konfigurasi subagent dan profil peran yang sudah ada. Skill `customize-opencode` yang disebut AGENTS.md tidak ditemukan di katalog project maupun direktori skill global yang diperiksa; runtime OpenCode/skill tidak tersedia pada sesi ini. Profil belum dibuktikan dimuat atau didelegasikan oleh OpenCode. Gate invokasi skill formal tetap BLOCKED; artifact ini adalah persiapan konfigurasi yang diminta pengguna. Tidak ada source/asset game atau permission yang diubah, sehingga Play+Execute tidak berlaku untuk authoring profil ini.
