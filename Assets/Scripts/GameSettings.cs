using UnityEngine;

public static class GameSettings
{
    private static bool _calmMode = false;

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

    static GameSettings()
    {
        _calmMode = PlayerPrefs.GetInt("CalmMode", 0) == 1;
    }
}
