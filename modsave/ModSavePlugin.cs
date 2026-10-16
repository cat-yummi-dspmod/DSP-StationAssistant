using System.IO;
using BepInEx;
using crecheng.DSPModSave;

namespace StationAssistant.ModSave
{
    [BepInPlugin(PluginGuid, "Station Assistant Mod Save", "0.1.0")]
    [BepInDependency("com.yummi.dsp.stationassistant")]
    [BepInDependency(DSPModSavePlugin.MODGUID)]
    public class ModSavePlugin : BaseUnityPlugin, IModCanSave
    {
        public const string PluginGuid = "com.yummi.dsp.stationassistant.modsave";

        public void Export(BinaryWriter writer)
        {
            ModSaveBridge.Export(writer);
        }

        public void Import(BinaryReader reader)
        {
            ModSaveBridge.Import(reader);
        }

        public void IntoOtherSave()
        {
            ModSaveBridge.IntoOtherSave();
        }
    }
}
