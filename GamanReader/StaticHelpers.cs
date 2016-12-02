using System.Windows.Input;

namespace GamanReader
{
    static class StaticHelpers
    {
#if DEBUG
        public const string TempFolder = "..\\Release\\Stored Data\\Temp";
        public const string ConfigPath = "..\\Release\\Stored Data\\config.xml";
        public const string StoredDataFolder = "..\\Release\\Stored Data";
#else
        public const string TempFolder = "Stored Data\\Temp";
        public const string ConfigPath = "Stored Data\\config.xml";
        public const string StoredDataFolder = "Stored Data";
#endif
        public const string ProgramName = "GamanReader";

        public static bool CtrlIsDown()
        {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        }

    }
}
