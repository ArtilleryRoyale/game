using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
#if !CC_DEBUG
using System.Collections;
using UnityEngine.Networking;
#endif

public class Log : MonoBehaviour
{
    private static Log Instance;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Critical log are sent on the server and produce an error entry in the log, active on production
    /// </summary>
    public static void Critical(
        string tag, string text,
        [CallerFilePath] string callerFilePath = "",
        [CallerMemberName] string callerMemberName = "",
        [CallerLineNumber] int callerLineNumber = 0
    )
    {
        if (Instance == null) return;

#if CC_DEBUG
        // Colors
        text = "<color=cyan>[" + tag + "]</color> <color=white>" + text + "</color>";
        text += " (frame: " + Time.frameCount + ")";
#else
        // Extra info
        callerFilePath = Path.GetFileName(callerFilePath);
        text += " (" + callerFilePath + ":" + callerLineNumber + " " + callerMemberName + "())";
        text = "[" + tag + "] " + text;
#endif

        Debug.LogError(text);
#if !CC_DEBUG
        Instance.StartCoroutine(GetRequest("https://artilleryroyale.com/log/?data=" + UnityWebRequest.EscapeURL(text)));
        IEnumerator GetRequest(string uri) {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri)) {
                yield return webRequest.SendWebRequest();
            }
        }
#endif
    }

    /// <summary>
    /// Error log produce an error entry in the log, active on production
    /// </summary>
    public static void Error(
        string tag, string text,
        [CallerFilePath] string callerFilePath = "",
        [CallerMemberName] string callerMemberName = "",
        [CallerLineNumber] int callerLineNumber = 0
    )
    {
        if (Instance == null) return;

#if CC_DEBUG
        // Colors
        text = "<color=cyan>[" + tag + "]</color> <color=white>" + text + "</color>";
        text += " (frame: " + Time.frameCount + ")";
#else
        // Extra info
        callerFilePath = Path.GetFileName(callerFilePath);
        text += " (" + callerFilePath + ":" + callerLineNumber + " " + callerMemberName + "())";
        text = "[" + tag + "] " + text;
#endif

        Debug.LogError(text);
    }

    /// <summary>
    /// Message log produce a log entry in the log, deactivated on production
    /// </summary>
    public static void Message(
        string tag, string text, bool skipIfTrue = false,
        [CallerFilePath] string callerFilePath = "",
        [CallerMemberName] string callerMemberName = "",
        [CallerLineNumber] int callerLineNumber = 0
    )
    {
        if (skipIfTrue) return;
#if !CC_DEBUG
        return;
#endif
        if (Instance == null) return;

#if CC_DEBUG
        // Colors
        text = "<color=cyan>[" + tag + "]</color> <color=white>" + text + "</color>";
        text += " (frame: " + Time.frameCount + ")";
        // Extra info
#else
        callerFilePath = Path.GetFileName(callerFilePath);
        text += " (" + callerFilePath + ":" + callerLineNumber + " " + callerMemberName + "())";
        text = "[" + tag + "] " + text;
#endif

        Debug.Log(text);
    }
}
