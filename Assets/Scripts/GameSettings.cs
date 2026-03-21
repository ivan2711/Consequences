using UnityEngine;

public static class GameSettings
{
    private static bool _calmMode = false;
    private static bool _showHints = true;

    public static bool CalmMode
    {
        get { return _calmMode; }
        set
        {
            _calmMode = value;
            PlayerPrefs.SetInt("CalmMode", value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool ShowHints
    {
        get { return _showHints; }
        set
        {
            _showHints = value;
            PlayerPrefs.SetInt("ShowHints", value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    static GameSettings()
    {
        _calmMode = PlayerPrefs.GetInt("CalmMode", 0) == 1;
        _showHints = PlayerPrefs.GetInt("ShowHints", 1) == 1;
    }
}
