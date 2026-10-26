# 09 — Technical Specification

- Engine: **Unity 6**
- Render target: 1080p, 60 FPS
- Input: keyboard/mouse (utama); controller support = target berikutnya
- Plugins: DOTween, Cinemachine
- Save: checkpoint-based, tanpa manual save (scope awal)
- Scene architecture: stage-based, additive loading bila perlu
- Data-driven content: evidence, hotspot, item, objective, dialog, puzzle → **ScriptableObject / data asset setara**
- Audio: mixer groups — Master, Music, SFX, Ambience, Dialogue/UI
- Localization-ready: teks pemain tidak boleh di-hardcode di scene

## Sistem Utama
1. Player Movement Controller
2. Interaction Raycast / Focus System
3. 3D Inspection Presenter
4. Evidence Database & Evidence Log
5. Hand/Pocket Inventory
6. Objective & Chapter State
7. Puzzle & Trigger Framework
8. Health, Stamina, Damage, & Trap System
9. Checkpoint Save/Load
10. Audio Surface & Environment System

## Target Performance
- 60 FPS stabil pada hardware minimum (TBD setelah vertical slice)
- Evidence model: LOD atau asset khusus inspection sesuai kebutuhan
- Lighting: baked/mixed untuk mayoritas stage; dynamic light dibatasi ke flashlight & event penting
- Audio source, decal, particle, pooled object: punya budget per area
- Spek minimum/rekomendasi PC: TBD setelah profiling

## Batasan untuk Agent
- Implementasi sistem baru wajib mengikuti pola data-driven (ScriptableObject/data asset) untuk evidence, item, objective, dialog, puzzle — jangan hardcode konten ke scene.
- Jangan mengasumsikan dukungan console atau multiplayer — di luar scope teknis awal (lihat `10-production-scope.md`).
- Semua teks UI/dialog harus lewat sistem localization-ready, bukan string literal langsung di komponen scene.
