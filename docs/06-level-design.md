# 06 — Level Design

## Struktur
Room-based / stage-based dengan checkpoint. Setiap lokasi punya: objective investigasi utama, optional evidence, environmental story, dan gerbang progres (terbuka lewat deduksi atau item).

## Urutan Lokasi
1. Crime Scene Alley
2. Motel & Kamar 201
3. Jalan Sepi
4. Gas Station
5. Pine Forest / Route 9
6. Old Wooden House
7. Basement
8. Final Confrontation

## Prinsip Layout
- Landmark visual untuk orientasi tanpa mini-map.
- Critical path jelas; optional evidence mendorong eksplorasi (bukan wajib).
- Backtracking dibatasi, selalu memberi perubahan keadaan/informasi baru.
- Rumah Adrian: looping corridor **terkendali**, bukan layout acak yang membingungkan.
- Checkpoint ditempatkan sebelum: perubahan besar, puzzle kompleks, confrontation.

## Batasan untuk Agent
- Urutan 8 lokasi di atas adalah kanon — perubahan urutan harus dieskalasi.
- Level baru di luar 8 lokasi ini dianggap **[USULAN BARU]** dan butuh approval, apalagi jika menyentuh scope MVP/vertical slice (lihat `10-production-scope.md`).
- Layout tanpa mini-map berarti agent harus mendesain landmark visual eksplisit — jangan mengandalkan UI navigasi sebagai pengganti.
