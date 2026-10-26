# Rules — Kepatuhan Agent terhadap GDD

Aturan ini melengkapi `AGENT.md`. Berlaku untuk semua agent/subagent (game design, narrative, systems, level, art, audio, technical, QA).

## R1. Rujukan Wajib
Setiap keputusan desain atau konten harus dapat ditelusuri ke file spesifik di `docs/`. Saat melaporkan hasil kerja, agent **wajib mencantumkan rujukan file** yang dipakai (mis. "berdasarkan `docs/03-core-gameplay-systems.md` §Aturan Evidence").

## R2. Larangan Fabrikasi Kanon
Agent dilarang mengarang fakta naratif, nama karakter, tanggal, angka gameplay final, atau keputusan desain yang belum ada di `docs/`. Jika dibutuhkan untuk menyelesaikan task, gunakan placeholder eksplisit dan tandai sebagai asumsi sementara.

## R3. Pilar sebagai Filter Pertama
Sebelum mendesain sistem/level/konten baru, jawab tiga pertanyaan berikut secara eksplisit dalam proses kerja (boleh internal, tidak selalu ditampilkan ke user):
1. Apakah ini konsisten dengan Grounded Horror Movement?
2. Apakah ini memperkuat Inspection sebagai Mekanik Utama?
3. Apakah ini disampaikan lewat tindakan pemain (Narrative Investigation), bukan exposition murni?
Jika salah satu jawaban "tidak", revisi ide sebelum lanjut.

## R4. Scope Gate
Sebelum implementasi fitur apa pun, cek `docs/10-production-scope.md`. Fitur yang masuk daftar "Di Luar Scope Awal" memerlukan eskalasi eksplisit sebelum dikerjakan — termasuk versi "kecil" atau "sederhana" dari fitur tersebut.

## R5. Anti Soft-Lock
Setiap sistem yang melibatkan item, evidence, atau progres wajib dicek terhadap aturan anti-frustrasi di `docs/03-core-gameplay-systems.md`. Desain yang berpotensi membuat pemain macet permanen harus ditolak atau diberi recovery path sebelum disetujui.

## R6. Rating & Konten Sensitif
Deskripsi kekerasan, gore, luka, atau kematian harus mengikuti batas implisit sesuai `docs/01-project-overview.md`. Saat ragu, pilih framing yang lebih implisit/tersirat, dan catat pilihan itu untuk review.

## R7. Konsistensi Lintas Dokumen
Saat mengubah atau menambah satu topik (mis. karakter baru), agent wajib mengecek dampaknya ke dokumen lain yang terkait (mis. narasi baru di Bab 3 harus konsisten dengan `04-narrative-and-characters.md`). Jangan membuat kontradiksi antar file `docs/`.

## R8. Transparansi Batas Kemampuan
Jika sebuah task membutuhkan keputusan gameplay/naratif yang secara wajar milik game director/lead designer manusia (bukan hanya soal referensi dokumen), agent menyatakan itu secara eksplisit alih-alih berpura-pura memutuskan atas nama tim.
