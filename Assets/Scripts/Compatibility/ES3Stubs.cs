// Stub for Easy Save 3 (ES3) - allows compilation without the ES3 plugin installed.
// Actual save/load functionality will NOT work with these stubs.

public static class ES3
{
    public static bool FileExists(string filePath) { return false; }
    public static void DeleteFile(string filePath) { }

    public static void Save<T>(string key, T value) { }
    public static void Save<T>(string key, T value, string filePath) { }

    public static T Load<T>(string key) { return default; }
    public static T Load<T>(string key, T defaultValue) { return defaultValue; }
    public static T Load<T>(string key, string filePath) { return default; }
    public static T Load<T>(string key, string filePath, T defaultValue) { return defaultValue; }

    public static bool KeyExists(string key) { return false; }
    public static bool KeyExists(string key, string filePath) { return false; }

    public static void DeleteKey(string key) { }
    public static void DeleteKey(string key, string filePath) { }
}
