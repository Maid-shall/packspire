using UnityEngine;

namespace Packspire {
/// <summary>
/// Single boundary for the project's selected legacy Input Manager.
/// Do not call the new Input System beside this without migrating this adapter.
/// </summary>
public static class PackspireInput {
 public static bool DeveloperTogglePressed()=>Input.GetKeyDown(KeyCode.F10);
 public static bool CancelPressed()=>Input.GetKeyDown(KeyCode.Escape);
 public static bool PanModifierHeld()=>Input.GetKey(KeyCode.Space);
}
}
