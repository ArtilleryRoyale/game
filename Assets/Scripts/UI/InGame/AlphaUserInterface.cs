using TMPro;
using UnityEngine;

// TODO prio 2 remove on final version
public class AlphaUserInterface : MonoBehaviour
{
    public static AlphaUserInterface Instance;

    [Header("References")]
    public GameManager gameManager = default;
    public TMP_Text versionText = default;

    private void Awake()
    {
        Instance = this;
        versionText.text = "Early Access - " + Application.version.Replace("0.", "v");
    }

    public void discordAction()
    {
        Application.OpenURL("https://artilleryroyale.com/discord");
    }

    public void steamAction()
    {
        Application.OpenURL("https://artilleryroyale.com/steam");
    }
}
