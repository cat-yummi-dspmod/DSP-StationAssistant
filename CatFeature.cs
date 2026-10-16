namespace StationAssistant
{
    internal static class CatFeature
    {
        internal static bool Enabled { get; private set; }

        internal static void Apply()
        {
            Enabled = Settings.CatText == "我是一只猫";
            if (Enabled)
            {
                Plugin.Log.LogInfo("隐藏功能入口已打开，功能尚未定义。");
            }
        }
    }
}
