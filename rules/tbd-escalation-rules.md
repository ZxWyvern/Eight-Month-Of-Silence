# Rules — Eskalasi Item TBD

## Kategori TBD
1. **TBD Diketahui** — sudah terdaftar di `docs/12-references-and-open-tbd.md`.
2. **TBD Baru** — gap desain yang ditemukan agent saat bekerja, belum tercatat di mana pun.

## Alur untuk TBD Diketahui
1. Lanjutkan task pada bagian yang tidak bergantung pada item TBD tersebut.
2. Tandai bagian output yang bergantung pada TBD dengan penanda eksplisit, contoh:
   `<!-- TBD#6: angka damage belum final, nilai di bawah ini estimasi kerja -->`
3. Jika relevan, sertakan 2–3 opsi dengan justifikasi singkat (berbasis pilar desain, referensi kreatif, atau genre pembanding) agar pemilik proyek bisa memutuskan cepat.
4. Jangan menghapus status TBD dari `docs/12-references-and-open-tbd.md` — hanya pemilik proyek yang menutup item TBD.

## Alur untuk TBD Baru (gap yang belum tercatat)
1. **Berhenti** sebelum melanjutkan bagian task yang bergantung pada gap ini.
2. Jelaskan secara singkat: apa yang dibutuhkan, kenapa `docs/` tidak cukup, dan opsi yang memungkinkan.
3. Tunggu arahan sebelum melanjutkan — jangan berasumsi dan melanjutkan implementasi penuh.
4. Setelah mendapat arahan, sarankan agar item ini ditambahkan ke `docs/12-references-and-open-tbd.md` (atau dokumen relevan) supaya tidak menjadi keputusan implisit yang hilang jejak.

## Prinsip
- Lebih baik proses melambat sedikit karena eskalasi, daripada progres desain berjalan cepat tapi berdasarkan asumsi yang salah dan menumpuk technical/design debt.
- Eskalasi bukan kegagalan agent — ini bagian dari alur kerja yang benar.
