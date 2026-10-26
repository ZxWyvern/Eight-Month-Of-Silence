# 08 — Art Direction & Audio Direction

## Visual Style
Low-poly 3D, proporsi & pencahayaan realistis. Detail visual diprioritaskan pada: evidence, tangan pemain, luka penting, material lingkungan, objek yang bisa diinspeksi. Model sederhana didukung silhouette, lighting, texture wear, staging kuat.

## Mood & Color Script per Lokasi
| Lokasi | Palet / Mood |
|---|---|
| Crime scene | Biru dingin, amber lampu jalan, refleksi aspal basah |
| Motel | Kuning kusam, merah gelap, ruang sempit & tercemar |
| Jalan & gas station | Ruang kosong, kabut, cahaya fluoresen keras |
| Hutan | Hitam kehijauan, jarak pandang rendah, silhouette pohon |
| Rumah Adrian | Cokelat gelap, hitam, cahaya lokal terbatas |
| Basement | Beton dingin, karat, bayangan keras, rasa kehampaan |

## Musik
Dark orchestral ambient, dipakai hemat. Area investigasi didominasi ambience realistis. Musik naik saat pemain menemukan hubungan penting/masuk bahaya, lalu bisa hilang total di rumah Adrian agar bunyi kecil terasa dominan.

## Environment & Footsteps
- Ambience: hujan, tetesan air, angin, daun, ranting, kendaraan jauh, dengungan listrik, kayu berderit.
- Footstep: aspal basah, lantai kayu, daun kering, tanah hutan, beton basement.
- Occlusion & reverb berbeda per tipe ruang (terbuka, motel, rumah, lorong, basement).

## Investigation & Tension SFX
- Radio polisi, kamera, dokumen, evidence pickup, inspection movement, flashlight, Evidence Log.
- Klik tali jebakan, papan bergerak, lantai retak, pintu terbanting, static radio, tawa samar.
- **Audio cue jebakan harus bisa dipelajari (learnable), bukan sekadar mengejutkan.**

## Combat SFX
Tembakan pistol/senapan; pukulan, benturan tubuh, crowbar pada material berbeda; napas berat, rasa sakit, reload, casing, room reflection. Final confrontation memprioritaskan clarity arah suara & intensitas ruangan.

## Batasan untuk Agent
- Palet warna per lokasi di atas adalah target art direction — brief art/environment baru harus merujuk tabel ini.
- Audio jebakan wajib didesain agar bisa dipelajari pemain (pola berulang, cue konsisten) — jumpscare murni tanpa pola dilarang, sesuai risiko di `11-risks-and-dod.md`.
