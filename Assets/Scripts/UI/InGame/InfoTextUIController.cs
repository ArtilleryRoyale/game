using System.Collections.Generic;
using CC;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InfoTextUIController : MonoBehaviour
{
    #region Fields

    [SerializeField] private TMP_Text text = default;

    #endregion

    public void Init()
    {
        AutoReplace();
    }

    public void InitManual()
    {
        Dictionary<string, string> bindings = new Dictionary<string, string>();
        foreach (var kv in bindings) {
            text.text = text.text.Replace("[" + kv.Key + "]", Controls(kv.Value));
            // Log.Message("InfoText", "Will replace [" + kv.Key + "] with " + Controls(kv.Value));
        }
    }

    private void AutoReplace()
    {
        PlayerInput playerInput = GameManager.Instance.PlayerOne.PlayerInput;
        foreach (var currentActionName in new string[]{"Move", "Drop", "Jump", "Aim", "Fire", "Weapon Selector", "Move View", "Zoom View"}) {
            string currentActionBindings = "";
            foreach (InputBinding binding in playerInput.actions[currentActionName].bindings) {
                if (binding.isComposite) continue; // Ok but we want to keep modifiers composites
                if (binding.isPartOfComposite) {
                    if (currentActionName == "Move View" /* Special case */) {
                        currentActionBindings += "+";
                    } else {
                        currentActionBindings += "&";
                    }
                } else {
                    currentActionBindings += "|";
                }
                currentActionBindings += Controls(binding.ToDisplayString());
            }
            currentActionBindings = currentActionBindings
                // Remove first separator
                .Trim('&', '|', '+')
                // Change machine separator to humain text
                .Replace("&", " and ")
                .Replace("|", " or ")
                .Replace("+", " + ")
            ;
            text.text = text.text.Replace("[" + currentActionName + "]", currentActionBindings);
            // Log.Message("InfoText", "Will replace [" + currentActionName + "] with " + currentActionBindings);
        }
    }

    public static string Controls(string name)
    {
        name = name.ToLower();
        name = name.Replace("/x", "").Replace("/y", ""); // Clean axes
        name = name.Replace(" ", "_");
        switch (name) {
            // Keyboard
            case "a":
            case "d":
            case "s":
            case "w":
            case "enter":
            case "space":
            case "tab":
            case "direction":
            case "backspace":
            case "left_arrow":
            case "right_arrow":
            case "up_arrow":
            case "down_arrow":
            case "shift":
            case "wasd":
                return "<sprite=\"controls\" name=\"keyboard_" + name + "\">";
            case "-":
                return "<sprite=\"controls\" name=\"keyboard_minus\">";
            case "=":
                return "<sprite=\"controls\" name=\"keyboard_plus\">";

            // Mouse
            case "rmb":
                return "<sprite=\"controls\" name=\"mouse_r\">";
            case "lmb":
                return "<sprite=\"controls\" name=\"mouse_l\">";
            case "delta":
            case "mouse":
            case "position":
                return "<sprite=\"controls\" name=\"mouse\">";
            case "scroll":
            case "scroll/y":
                return "<sprite=\"controls\" name=\"mouse_scroll\">";
            default:
                return "[" + name + "]";
        }
    }
}
