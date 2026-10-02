using System;
using System.Collections.Generic;
using System.Linq;
using ADOFAI;
using HarmonyLib;

namespace AdofaiPro.Patching
{
    /// <summary>
    /// <c>GCS.levelEventsInfo</c> 刚由 <c>ADOStartup</c> 建好时立刻注册。
    /// 这样即使玩家不打开编辑器、直接游玩含本事件的关卡，Decode 也能找到事件定义。
    /// </summary>
    [HarmonyPatch(typeof(ADOStartup), "SetupLevelEventsInfo")]
    internal static class SetupLevelEventsInfoPatch
    {
        internal static void Postfix()
        {
            CustomEvent.EnsureRegistered();
        }
    }

    /// <summary>
    /// 编辑器构建事件类别页签与事件按钮之前注册事件定义与图标。
    /// <c>LoadEditorProperties</c> 遍历 <c>GCS.levelEventsInfo</c> 生成按钮，是唯一的注入时机。
    /// </summary>
    [HarmonyPatch(typeof(scnEditor), nameof(scnEditor.LoadEditorProperties))]
    internal static class LoadEditorPropertiesPatch
    {
        internal static void Prefix()
        {
            CustomEvent.EnsureRegistered();
            CustomEvent.EnsureIcon();
        }

        /// <summary>构建完按钮后打一行诊断日志，方便确认按钮确实进了底部栏以及在第几页。</summary>
        internal static void Postfix(scnEditor __instance)
        {
            try
            {
                LevelEventCategory category = LevelEventCategory.VisualFx;
                List<LevelEventButton> buttons;
                if (__instance.eventButtons == null ||
                    !__instance.eventButtons.TryGetValue(category, out buttons) ||
                    buttons == null)
                {
                    Main.Log("[diag] bottom bar: no " + category + " button list.");
                    return;
                }

                List<LevelEventButton> mine = buttons.Where(b => b != null && (int)b.type == CustomEvent.TypeId).ToList();
                if (mine.Count == 0)
                {
                    Main.Log("[diag] bottom bar: " + category + " has " + buttons.Count + " buttons, custom MISSING.");
                    return;
                }

                LevelEventButton button = mine[0];
                Main.Log("[diag] bottom bar: " + category + " has " + buttons.Count +
                         " buttons, custom at page=" + button.page + " keyCode=" + button.keyCode +
                         " pageSize=11.");
            }
            catch (Exception e)
            {
                Main.Error("bottom bar diagnostics failed: " + e);
            }
        }
    }

    /// <summary>
    /// 让"选中砖 → 打开属性面板"的原版行为也覆盖本事件。
    ///
    /// 原版 <c>InspectorPanel.ShowTabsForFloor</c> 用 <c>Enum.GetValues(typeof(LevelEventType))</c>
    /// 决定默认打开哪个事件的面板；本事件的类型值（900）不在枚举里，因此当砖上只有本事件时
    /// 原版会选中 None（面板不开、selectedEventType 变成 None）。
    /// 这里在方法返回后兜底：若砖上确实有本事件而原版什么也没选中，就手动打开它，
    /// 使其与"砖上只有一个原版事件"的行为一致。
    /// </summary>
    [HarmonyPatch(typeof(InspectorPanel), nameof(InspectorPanel.ShowTabsForFloor))]
    internal static class ShowTabsForFloorPatch
    {
        internal static void Postfix(InspectorPanel __instance, int floorID)
        {
            if (!CustomEvent.AutoOpenOnTileSelect || __instance == null || !CustomEvent.IsRegistered)
            {
                return;
            }

            try
            {
                if (__instance.selectedEventType != LevelEventType.None)
                {
                    return;
                }

                scnEditor editor = scnEditor.instance;
                if (editor == null || editor.events == null)
                {
                    return;
                }

                List<LevelEvent> mine = editor.events
                    .Where(ev => ev != null && ev.floor == floorID && (int)ev.eventType == CustomEvent.TypeId)
                    .ToList();
                if (mine.Count == 0)
                {
                    return;
                }

                __instance.ShowPanel(CustomEvent.Type, 0);
            }
            catch (Exception e)
            {
                Main.Error("ShowTabsForFloor patch failed: " + e);
            }
        }
    }

    /// <summary>
    /// 兜底：任何事件按钮初始化前确保图标存在。
    /// 此时 <c>GCS.levelEventIcons</c> 已被游戏的 LoadLevelEventSprites 建好，追加安全。
    /// </summary>
    [HarmonyPatch(typeof(LevelEventButton), "Init")]
    internal static class LevelEventButtonInitPatch
    {
        internal static void Prefix()
        {
            CustomEvent.EnsureIcon();
        }
    }

    /// <summary>
    /// 事件标题 / 按钮悬停提示的本地化：原版会查 "editor.&lt;事件类型&gt;"，
    /// 我们的类型是 900，所以补上 "editor.900"。
    /// </summary>
    [HarmonyPatch(typeof(RDString), "GetWithCheck")]
    internal static class GetWithCheckPatch
    {
        internal static void Postfix(ref string __result, string key, ref bool exists)
        {
            if (key == "editor." + CustomEvent.DictKey || key == "editor." + CustomEvent.FileName)
            {
                exists = true;
                __result = "Tile Tint";
            }
        }
    }
}
