using System.Reflection;
using ADOFAI;
using HarmonyLib;

namespace AdofaiPro.Patching
{
    /// <summary>
    /// 让 <c>RDUtils.ParseEnum&lt;LevelEventType&gt;</c> 认识自定义事件名。
    ///
    /// 必要性：<c>scnEditor.LoadEditorProperties</c> 会对每个 <c>LevelEventInfo.name</c> 调用
    /// <c>ParseEnum</c> 来决定按钮绑定的类型；<c>LevelEvent.Decode</c> 也用同一函数解析文件里的
    /// eventType。枚举里没有 "SetTileTint"，所以必须在这里把它映射到 <c>(LevelEventType)900</c>。
    /// （数字串 "900" 本身能被 Enum.TryParse 解析，这里一并处理以防手写文件。）
    /// </summary>
    [HarmonyPatch]
    internal static class ParseEnumPatch
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(RDUtils), "ParseEnum", null, null)
                .MakeGenericMethod(typeof(LevelEventType));
        }

        internal static bool Prefix(string str, ref LevelEventType __result)
        {
            if (str == CustomEvent.FileName || str == CustomEvent.DictKey)
            {
                __result = CustomEvent.Type;
                return false;
            }

            return true;
        }
    }
}
