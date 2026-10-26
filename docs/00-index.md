# Index Dokumen Konteks — Eight Months of Silence

Folder ini adalah pecahan terstruktur dari Game Design Document (GDD) asli, dipisah per topik agar agent dapat memuat konteks secara efisien (tidak perlu membaca seluruh GDD untuk setiap task).

| File | Isi | Rujukan bab GDD asli |
|---|---|---|
| `01-project-overview.md` | Ringkasan proyek, high concept, target pemain, USP | Bab 1 |
| `02-design-pillars-vision.md` | Visi, 3 pilar desain, prinsip pengalaman | Bab 2 |
| `03-core-gameplay-systems.md` | Core loop, movement, inspection, inventory, investigasi, survival/combat | Bab 3–8 |
| `04-narrative-and-characters.md` | Premis, tema, storytelling, karakter | Bab 9, 11 |
| `05-chapter-structure.md` | Struktur bab & progression (Bab 1–5) | Bab 10 |
| `06-level-design.md` | Struktur level, urutan lokasi, prinsip layout | Bab 12 |
| `07-ui-ux.md` | HUD, menu, input default | Bab 13 |
| `08-art-and-audio-direction.md` | Visual style, color script, musik, SFX | Bab 14–15 |
| `09-technical-spec.md` | Engine, sistem utama, target performa | Bab 16 |
| `10-production-scope.md` | MVP, vertical slice, di luar scope | Bab 17 |
| `11-risks-and-dod.md` | Risiko & mitigasi, Definition of Done | Bab 18–19 |
| `12-references-and-open-tbd.md` | Referensi kreatif, keputusan terbuka/TBD | Bab 20–21 |

## Cara Memuat Konteks

- **Task naratif/dialog/lore** → baca `04-narrative-and-characters.md` + `05-chapter-structure.md`.
- **Task sistem gameplay (movement, inventory, inspection)** → baca `03-core-gameplay-systems.md` + `02-design-pillars-vision.md`.
- **Task level/environment** → baca `06-level-design.md` + `05-chapter-structure.md` + `08-art-and-audio-direction.md`.
- **Task UI/UX** → baca `07-ui-ux.md` + `03-core-gameplay-systems.md`.
- **Task teknis/engine/arsitektur** → baca `09-technical-spec.md`.
- **Task scoping/roadmap/prioritas** → baca `10-production-scope.md` + `11-risks-and-dod.md`.
- **Task apa pun sebelum finalisasi** → cek `12-references-and-open-tbd.md` untuk item yang masih terbuka.

Selalu mulai dari file ini, lalu buka hanya file yang relevan. Jangan memuat seluruh `docs/` untuk task kecil.
