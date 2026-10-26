# 03 — Core Gameplay Systems

## Core Loop — Investigasi
Explore → Notice → Interact → Inspect → Collect Evidence → Connect Clues → Unlock Information → Progress Story

## Core Loop — Survival Horror
Explore area berbahaya → Observe & baca tanda jebakan → Kelola item tangan/kantong → Gunakan bukti/tool untuk puzzle → Hindari/nonaktifkan/selamat dari ancaman → Bergerak mendekati Adrian

## Struktur Tensi
Bab awal = keamanan relatif & prosedur. Bab tengah = saksi, identitas pelaku, ketidakpastian. Bab akhir = dukungan polisi berkurang, ruang menyempit, resource terbatas, setiap langkah berisiko fisik.

## Player Movement & Game Feel
- **Kemampuan dasar**: Walk (mode utama), Sprint (konsumsi stamina, tidak secepat FPS biasa), Crouch (obstacle rendah, senyap, berlindung), Limited Jump (hanya obstacle masuk akal), Look/lean situasional.
- **Feel**: akselerasi/deselerasi halus, camera sway & head bob ringan (anti motion-sickness), camera inertia, FOV naik sedikit saat sprint, napas berat saat stamina rendah/damage, footstep sesuai material, jump ada cooldown + landing impact ringan, crouch jauh lebih lambat dari walk. Head bob/camera shake/FOV harus togglable di Settings.
- **Stats**: Health (berkurang dari trap/serangan/bahaya), Stamina (pulih setelah jeda), State modifiers (wounded state memengaruhi napas/langkah/stabilitas kamera). **Angka tuning final berstatus TBD** — jangan finalisasi angka ini.

## Interaction & 3D Item Inspection
Flow: World Item → Focus Prompt → Pick Up → Inspection Mode → Rotate/Zoom → Examine Hotspot → Discover Information → Take/Leave.

- Inspection Mode: dunia diredupkan/DOF ringan, bisa pause atau slow-time tergantung konteks; drag mouse = rotate 360°; scroll = zoom; hotspot hanya muncul di sudut/jarak yang valid; examine memicu observasi James + update Evidence Log jika relevan; item bisa diambil/ditinggal setelah pemeriksaan awal; inspection bisa dibatalkan tanpa kehilangan progres hotspot yang sudah valid.
- Contoh referensi: jam tangan berdarah dengan hotspot noda darah, kaca pecah, ukiran "A.T.", jarum berhenti 23.47 → Evidence Log mencatat estimasi waktu & hubungan ke Adrian.

### Aturan Evidence
- **Critical Evidence** → masuk Evidence Log, TIDAK memakai slot fisik, TIDAK BISA dibuang (anti soft-lock).
- **Tool/consumable/key kecil/weapon** → memakai slot fisik.
- **Optional Evidence** → memperkaya lore, dialog, kualitas deduksi.
- Setiap evidence punya: nama, deskripsi, hotspot, fakta yang ditemukan, dependency, consequence.

## Inventory — Hands & Pocket
- Left Hand: 1 Active Item Slot · Right Hand: 1 Active Item Slot · Pocket: 4 Storage Slots.
- Total kapasitas fisik 6 item, hanya 2 (di tangan) yang bisa langsung dipakai.
- Item aktif contoh: flashlight, camera, pistol, crowbar, radio, evidence tool, key item tertentu. Kombinasi tangan sesuai kompatibilitas (mis. flashlight+pistol, flashlight+crowbar).
- Aksi: Pocket→Hand, Hand→Pocket, Hand↔Hand, Hand→Drop, Pocket→Drop, Replace Left/Right Hand saat penuh, Store in Pocket, Leave.

### Aturan Anti-Frustrasi (WAJIB)
- Objective item wajib tidak boleh hilang permanen.
- Jika objective item berbentuk fisik → wajib ada recovery point atau auto-return ke lokasi aman.
- UI wajib menampilkan alasan saat item tidak bisa digunakan/dipindahkan.
- Ganti tangan harus cepat, tanpa menu bertingkat.
- Pause inventory kontekstual: pause penuh di area aman, real-time/slow-time di area berbahaya.

## Sistem Investigasi & Deduksi
- **Evidence Log**: mencatat bukti utama, foto, observasi, saksi, hubungan antar-petunjuk; memisahkan Facts / Leads / Unresolved Questions; menandai info baru tanpa spam notifikasi; menampilkan sumber penemuan; tidak memberi jawaban penuh sebelum syarat deduksi terpenuhi.
- **Reconstruction**: pemain menyusun fakta untuk merekonstruksi kejadian; jawaban salah = feedback logis + retry, TIDAK kehilangan progress; jawaban benar membuka deduksi/objective/lokasi baru.
- **Puzzle**: Environmental, Evidence-based, Combination/Key, Trap Observation. **Tidak boleh pixel-hunting** — clue penting wajib punya visual & audio readability.

## Survival & Combat
- Filosofi: combat situasional, lambat, berisiko. James adalah detektif, bukan prajurit aksi. Senjata untuk bertahan/menciptakan peluang, bukan clear arena.
- Sistem dasar: aim & fire firearm terbatas; melee kontekstual (crowbar/objek); ammo & healing sangat terbatas; damage punya feedback audio/camera/wounded state; final confrontation menggabungkan observasi + environment + aksi langsung.
- **Eksplisit di luar scope awal**: grinding, loot rarity, skill tree, encounter combat berulang.

## Batasan untuk Agent
- Jangan mengusulkan sistem crafting, skill tree, atau loot rarity — ini eksplisit dilarang di scope awal.
- Jangan membuat evidence baru tanpa mengisi kelima atribut wajib (nama, deskripsi, hotspot, fakta, dependency, consequence).
- Jangan menghapus aturan anti soft-lock demi "tantangan" — ini adalah hard requirement, bukan opsi tuning.
