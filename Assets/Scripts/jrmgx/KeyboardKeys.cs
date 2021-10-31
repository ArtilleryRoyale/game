using UnityEngine;

namespace Jrmgx.Helpers
{
    public static class KeyboardKeys
    {
        public static string ModifierToCaption(Event keyEvent)
        {
            string key = "";
            if (keyEvent.control) {
                key += "⌃+";
            }

            if (keyEvent.command) {
                key += "⌘+";
            }

            if (keyEvent.alt) {
                key += "⌥+";
            }

            if (keyEvent.shift) {
                key += "⇧+";
            }

            return key;
        }

        public static string ToCaption(KeyCode key)
        {
            switch (key) {
                case KeyCode.None: return null;
                case KeyCode.Backspace: return "⌫";
                case KeyCode.Tab: return "Tab";
                case KeyCode.Clear: return "Clear";
                case KeyCode.Return: return "Enter";
                case KeyCode.Pause: return "Pause";
                case KeyCode.Escape: return "Esc";
                case KeyCode.Space: return "Space";
                case KeyCode.Exclaim: return "!";
                case KeyCode.DoubleQuote: return "\"";
                case KeyCode.Hash: return "#";
                case KeyCode.Dollar: return "$";
                case KeyCode.Ampersand: return "&";
                case KeyCode.Quote: return "'";
                case KeyCode.LeftParen: return "(";
                case KeyCode.RightParen: return ")";
                case KeyCode.Asterisk: return "*";
                case KeyCode.Plus: return "+";
                case KeyCode.Comma: return ",";
                case KeyCode.Minus: return "-";
                case KeyCode.Period: return ".";
                case KeyCode.Slash: return "/";
                case KeyCode.Alpha0: return "0";
                case KeyCode.Alpha1: return "1";
                case KeyCode.Alpha2: return "2";
                case KeyCode.Alpha3: return "3";
                case KeyCode.Alpha4: return "4";
                case KeyCode.Alpha5: return "5";
                case KeyCode.Alpha6: return "6";
                case KeyCode.Alpha7: return "7";
                case KeyCode.Alpha8: return "8";
                case KeyCode.Alpha9: return "9";
                case KeyCode.Colon: return ":";
                case KeyCode.Semicolon: return ";";
                case KeyCode.Less: return "<";
                case KeyCode.Equals: return "=";
                case KeyCode.Greater: return ">";
                case KeyCode.Question: return "?";
                case KeyCode.At: return "@";
                case KeyCode.LeftBracket: return "[";
                case KeyCode.Backslash: return "\\";
                case KeyCode.RightBracket: return "]";
                case KeyCode.Caret: return "^";
                case KeyCode.Underscore: return "_";
                case KeyCode.BackQuote: return "`";
                case KeyCode.A: return "A";
                case KeyCode.B: return "B";
                case KeyCode.C: return "C";
                case KeyCode.D: return "D";
                case KeyCode.E: return "E";
                case KeyCode.F: return "F";
                case KeyCode.G: return "G";
                case KeyCode.H: return "H";
                case KeyCode.I: return "I";
                case KeyCode.J: return "J";
                case KeyCode.K: return "K";
                case KeyCode.L: return "L";
                case KeyCode.M: return "M";
                case KeyCode.N: return "N";
                case KeyCode.O: return "O";
                case KeyCode.P: return "P";
                case KeyCode.Q: return "Q";
                case KeyCode.R: return "R";
                case KeyCode.S: return "S";
                case KeyCode.T: return "T";
                case KeyCode.U: return "U";
                case KeyCode.V: return "V";
                case KeyCode.W: return "W";
                case KeyCode.X: return "X";
                case KeyCode.Y: return "Y";
                case KeyCode.Z: return "Z";
                case KeyCode.Delete: return "Delete";
                case KeyCode.Keypad0: return "[0]";
                case KeyCode.Keypad1: return "[1]";
                case KeyCode.Keypad2: return "[2]";
                case KeyCode.Keypad3: return "[3]";
                case KeyCode.Keypad4: return "[4]";
                case KeyCode.Keypad5: return "[5]";
                case KeyCode.Keypad6: return "[6]";
                case KeyCode.Keypad7: return "[7]";
                case KeyCode.Keypad8: return "[8]";
                case KeyCode.Keypad9: return "[9]";
                case KeyCode.KeypadPeriod: return "[.]";
                case KeyCode.KeypadDivide: return "[/]";
                case KeyCode.KeypadMultiply: return "[*]";
                case KeyCode.KeypadMinus: return "[-]";
                case KeyCode.KeypadPlus: return "[+]";
                case KeyCode.KeypadEnter: return "[Enter]";
                case KeyCode.KeypadEquals: return "[=]";
                case KeyCode.UpArrow: return "↑";
                case KeyCode.DownArrow: return "↓";
                case KeyCode.RightArrow: return "→";
                case KeyCode.LeftArrow: return "←";
                case KeyCode.Insert: return "Ins";
                case KeyCode.Home: return "Home";
                case KeyCode.End: return "End";
                case KeyCode.PageUp: return "Page Up";
                case KeyCode.PageDown: return "Page Down";
                case KeyCode.F1: return "F1";
                case KeyCode.F2: return "F2";
                case KeyCode.F3: return "F3";
                case KeyCode.F4: return "F4";
                case KeyCode.F5: return "F5";
                case KeyCode.F6: return "F6";
                case KeyCode.F7: return "F7";
                case KeyCode.F8: return "F8";
                case KeyCode.F9: return "F9";
                case KeyCode.F10: return "F10";
                case KeyCode.F11: return "F11";
                case KeyCode.F12: return "F12";
                case KeyCode.F13: return "F13";
                case KeyCode.F14: return "F14";
                case KeyCode.F15: return "F15";
                case KeyCode.Numlock: return "Num Lock";
                case KeyCode.CapsLock: return "Cap Lock";
                case KeyCode.ScrollLock: return "Scr Loc";
                case KeyCode.RightShift: return "Right ⇧";
                case KeyCode.LeftShift: return "Left ⇧";
                case KeyCode.RightControl: return "Right ⌃";
                case KeyCode.LeftControl: return "Left ⌃";
                case KeyCode.RightAlt: return "Right ⌥";
                case KeyCode.LeftAlt: return "Left ⌥";
                case KeyCode.Mouse0: return "Mouse 0";
                case KeyCode.Mouse1: return "Mouse 1";
                case KeyCode.Mouse2: return "Mouse 2";
                case KeyCode.Mouse3: return "Mouse 3";
                case KeyCode.Mouse4: return "Mouse 4";
                case KeyCode.Mouse5: return "Mouse 5";
                case KeyCode.Mouse6: return "Mouse 6";
                case KeyCode.JoystickButton0: return "(A)";
                case KeyCode.JoystickButton1: return "(B)";
                case KeyCode.JoystickButton2: return "(X)";
                case KeyCode.JoystickButton3: return "(Y)";
                case KeyCode.JoystickButton4: return "(RB)";
                case KeyCode.JoystickButton5: return "(LB)";
                case KeyCode.JoystickButton6: return "(Back)";
                case KeyCode.JoystickButton7: return "(Start)";
                case KeyCode.JoystickButton8: return "(LS)";
                case KeyCode.JoystickButton9: return "(RS)";
                case KeyCode.JoystickButton10: return "J10";
                case KeyCode.JoystickButton11: return "J11";
                case KeyCode.JoystickButton12: return "J12";
                case KeyCode.JoystickButton13: return "J13";
                case KeyCode.JoystickButton14: return "J14";
                case KeyCode.JoystickButton15: return "J15";
                case KeyCode.JoystickButton16: return "J16";
                case KeyCode.JoystickButton17: return "J17";
                case KeyCode.JoystickButton18: return "J18";
                case KeyCode.JoystickButton19: return "J19";
            }
            string code = key.ToString();
            switch (code) {
                // French keyboard:
                case "160": return "é";
                case "161": return "§";
                case "162": return "ç";
                case "163": return "è";
                case "164": return "à";
                case "165": return "ù";
            }
            return key.ToString();
        }
    }
}
