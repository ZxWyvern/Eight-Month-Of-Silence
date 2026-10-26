# Playtests — vertical slice

Gate Production → Polish mewajibkan **minimal 3 sesi berbeda yang terdokumentasi**:
pengalaman pemain baru, sistem mid-game, dan kurva kesulitan.
Ketiganya **wajib dimainkan manusia** — sweep agent bukan pengganti
(`qa-plan.md`; dilarang memfabrikasi data pemain — GDD-002).

## Status sesi

| Sesi | Fokus | Status |
|---|---|---|
| `session-01-new-player-experience.md` | Pemain baru: 2 menit pertama, objective terbaca? | PENDING |
| `session-02-mid-game-systems.md` | Inventory/inspection/puzzle/trap di tengah slice | PENDING |
| `session-03-difficulty-curve.md` | Tantangan adil? titik frustrasi? | PENDING |

## Cara menjalankan

1. Build/play `PunyaPandu` (buildIndex 0), tanpa panduan developer.
2. Minta pemain berpikir keras (think-aloud); catat WAKTU tiap kebuntuan.
3. Isi `playtest-template.md` → simpan sebagai `session-NN-<tanggal>.md`.
4. Verdict per sesi: FUN / OK / BLOCKED + 3 temuan teratas.
5. Aturan keras: kebuntuan >50% penguji di titik yang sama = confusion loop → BLOCKED.

## Yang ditanyakan ke pemain (inti)

- Apa yang kamu pikir harus dilakukan dalam 2 menit pertama?
- Bukti mana yang kamu temukan lewat observasi vs kebetulan?
- Apakah perbedaan tangan/kantong/Evidence Log jelas?
- Puzzle bisa dipecahkan dari clue yang ada? tebakan liar?
- Momen paling tegang / paling membosankan?
- Apakah transisi investigasi→survival terasa sebagai eskalasi (`docs/11` DoD)?
