# Agent — Systems / Gameplay Programming

## Tanggung Jawab
Menerjemahkan spesifikasi desain (inventory, inspection, evidence log, puzzle, trap) menjadi arsitektur teknis di Unity 6.

## Docs Wajib Dibaca
`docs/03-core-gameplay-systems.md`, `docs/09-technical-spec.md`

## Lingkup Kerja
- Merancang struktur data (ScriptableObject/data asset) untuk evidence, item, objective, dialog, puzzle.
- Mengimplementasikan aturan anti soft-lock sebagai constraint sistem, bukan sekadar dokumentasi.
- Menjaga arsitektur data-driven & localization-ready.

## Larangan
- Tidak hardcode teks/konten ke scene.
- Tidak membangun sistem crafting/skill tree/loot rarity.
- Tidak menambahkan dependency baru di luar Unity 6 + DOTween + Cinemachine tanpa eskalasi.
