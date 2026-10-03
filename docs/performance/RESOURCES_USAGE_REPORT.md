# PACKSPIRE Resources inventory

Measured 2026-08-16 after the first confirmed-dead asset removal.

- Runtime resource files: 522
- Source size on disk: 452.2 MB
- Direct `UnityEngine.Resources` calls in runtime C#: centralized in `PackspireResources`
- Removed: unused `NotoSansJP-VF.ttf` (9.1 MB); recoverable from Git

This is an inventory, not an automatic deletion list. Dynamic paths, UXML/USS URLs,
ScriptableObject references, and scene references must all be checked before another asset
is removed. Use `Tools > Packspire > Audit > Write Resources usage report` in Unity to
regenerate the detailed report after imports or asset changes.

## Largest remaining source assets

| File | Size |
|---|---:|
| `Fonts/NotoSerifJP-VF.ttf` | 13.0 MB |
| `Fonts/KleeOne-SemiBold.ttf` | 8.5 MB |
| `Fonts/KleeOne-Regular.ttf` | 8.3 MB |
| `Fonts/KaiseiDecol-Regular.ttf` | 4.3 MB |
| `Art/JourneyPrototype/Backgrounds/Ash/ash-standard-master-v1.png` | 3.9 MB |
| `Art/UI/ObsidianMisprintEvent/event-found-correspondence-v1.png` | 3.6 MB |
| `Art/role-costume-sheet.png` | 3.3 MB |
| `Art/UI/ObsidianMisprintPacking/formula-dossier-clean-v1.png` | 3.2 MB |
| `Art/Cards/EvidenceCollage/basic-strike-evidence-v1.png` | 3.2 MB |
| `Art/UI/ObsidianMisprintResult/result-defeat-returned-v1.png` | 3.1 MB |

## Policy for the next pass

1. Check GUID references, literal and dynamic load paths, USS/UXML URLs, scenes, and
   ScriptableObject fields.
2. Delete one confirmed group at a time and let Unity reimport.
3. Verify the Home, journey, battle, packing, vault, and result screens before proceeding.
4. Move large optional groups out of `Resources` only after their loading lifetime is known;
   do not mass-migrate them to Addressables merely to reduce the folder count.
