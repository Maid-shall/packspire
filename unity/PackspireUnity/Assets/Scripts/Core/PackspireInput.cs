using UnityEngine;

namespace Packspire {
/// <summary>
/// Single boundary for the project's selected legacy Input Manager.
/// Do not call the new Input System beside this without migrating this adapter.
/// </summary>
public static class PackspireInput {
 public static bool DeveloperTogglePressed()=>Input.GetKeyDown(KeyCode.F10);
 public static bool CancelPressed()=>Input.GetKeyDown(KeyCode.Escape)||
  Input.GetKeyDown(KeyCode.JoystickButton1);
 public static bool JourneyLedgerPressed()=>Input.GetKeyDown(KeyCode.Tab)||
  Input.GetKeyDown(KeyCode.JoystickButton4);
 public static bool JourneyPausePressed()=>Input.GetKeyDown(KeyCode.Escape)||
  Input.GetKeyDown(KeyCode.JoystickButton7);
 public static bool PrimaryActionPressed()=>Input.GetKeyDown(KeyCode.Space)||
  Input.GetKeyDown(KeyCode.Return)||
  Input.GetKeyDown(KeyCode.KeypadEnter)||
  Input.GetKeyDown(KeyCode.JoystickButton0);
 public static bool SecondaryActionPressed()=>Input.GetKeyDown(KeyCode.JoystickButton1);
 public static bool FirstChoicePressed()=>Input.GetKeyDown(KeyCode.Alpha1)||
  Input.GetKeyDown(KeyCode.JoystickButton0);
 public static bool SecondChoicePressed()=>Input.GetKeyDown(KeyCode.Alpha2)||
  Input.GetKeyDown(KeyCode.JoystickButton1);
 public static bool NavigateLeftPressed()=>Input.GetKeyDown(KeyCode.A)||
  Input.GetKeyDown(KeyCode.LeftArrow);
 public static bool NavigateRightPressed()=>Input.GetKeyDown(KeyCode.D)||
  Input.GetKeyDown(KeyCode.RightArrow);
 public static bool JumpReactionPressed()=>Input.GetKeyDown(KeyCode.Space)||
  Input.GetKeyDown(KeyCode.JoystickButton2);
 public static bool BraceReactionPressed()=>
  Input.GetKeyDown(KeyCode.LeftShift)||
  Input.GetKeyDown(KeyCode.RightShift)||
  Input.GetKeyDown(KeyCode.JoystickButton3);
 public static bool RetryPressed()=>Input.GetKeyDown(KeyCode.R);
 public static bool PanModifierHeld()=>Input.GetKey(KeyCode.Space);
}
}
