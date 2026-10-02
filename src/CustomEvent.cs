using System;
using System.Collections.Generic;
using ADOFAI;
using UnityEngine;

namespace AdofaiPro
{
    /// <summary>
    /// 本 Mod 新增的关卡事件 <c>SetTileTint</c> 的定义与注册。
    ///
    /// ADOFAI 的事件系统是数据驱动的：每个事件类型只是一条 <see cref="LevelEventInfo"/>
    /// （含若干 <see cref="PropertyInfo"/>），实例是通用的 <see cref="LevelEvent"/>（floor + data 字典）。
    /// 由于 <see cref="LevelEventType"/> 是编译期固定的枚举，这里用一个枚举外的数值
    /// <see cref="TypeId"/>=900 作为事件类型，并：
    ///   1. 往 <c>GCS.levelEventsInfo</c> 注入一条 <see cref="LevelEventInfo"/>；
    ///   2. 往 <c>GCS.levelEventTypeString</c> 注入 enum→字符串 映射；
    ///   3. 往 <c>GCS.levelEventIcons</c> 注入图标；
    ///   4. 用 Harmony 补丁让 <c>RDUtils.ParseEnum&lt;LevelEventType&gt;</c> 认识事件名，
    ///      并让读写关卡文件时使用可读名字 "SetTileTint"（而不是 "900"）。
    /// </summary>
    internal static class CustomEvent
    {
        /// <summary>事件类型 ID（避开游戏 0..65 与已知 mod 的 801/802/810/812）。</summary>
        internal const int TypeId = 900;

        /// <summary>写进 .adofai 的可读事件名。</summary>
        internal const string FileName = "SetTileTint";

        /// <summary><c>GCS.levelEventsInfo</c> / <c>GCS.levelEventTypeString</c> 里使用的键。</summary>
        internal const string DictKey = "900";

        internal static readonly LevelEventType Type = (LevelEventType)TypeId;

        private static bool registered;
        private static Sprite icon;

        /// <summary>幂等注册。任何时候调用都安全（GCS 表还没建好时直接跳过）。</summary>
        internal static void EnsureRegistered()
        {
            try
            {
                if (GCS.levelEventsInfo == null)
                {
                    return;
                }

                if (!GCS.levelEventsInfo.ContainsKey(DictKey))
                {
                    GCS.levelEventsInfo[DictKey] = BuildInfo();
                    Main.Log("Registered custom event '" + FileName + "' as LevelEventType " + TypeId + ".");
                }

                if (GCS.levelEventTypeString != null)
                {
                    GCS.levelEventTypeString[Type] = DictKey;
                }

                MoveToFront();

                registered = true;
            }
            catch (Exception e)
            {
                Main.Error("EnsureRegistered failed: " + e);
            }
        }

        /// <summary>
        /// 把本事件在 <c>GCS.levelEventsInfo</c> 里的枚举顺序挪到最前。
        ///
        /// 原版 <c>scnEditor.LoadEditorProperties</c> 按该字典的枚举顺序给每个类别的按钮编号
        /// （<c>page = 序号 / 11</c>、<c>keyCode = 序号 % 11 + 1</c>），所以排在最后会让按钮
        /// 落到类别页签的最后一页（默认看不到）。挪到最前 ⇒ 永远在对应类别的第 0 页第一格。
        /// 就地重排（不替换字典对象），避免影响其它引用该字段的 mod。
        /// </summary>
        private static void MoveToFront()
        {
            Dictionary<string, LevelEventInfo> dict = GCS.levelEventsInfo;
            if (dict == null || !dict.ContainsKey(DictKey))
            {
                return;
            }

            // 已经在最前就不用动
            foreach (KeyValuePair<string, LevelEventInfo> pair in dict)
            {
                if (pair.Key == DictKey)
                {
                    return;
                }

                break;
            }

            var entries = new List<KeyValuePair<string, LevelEventInfo>>(dict);
            LevelEventInfo ours = dict[DictKey];
            entries.RemoveAll(e => e.Key == DictKey);
            entries.Insert(0, new KeyValuePair<string, LevelEventInfo>(DictKey, ours));

            dict.Clear();
            foreach (KeyValuePair<string, LevelEventInfo> pair in entries)
            {
                dict[pair.Key] = pair.Value;
            }
        }

        /// <summary>
        /// 选中一块砖时，若该砖上唯一的事件就是本事件，是否像原版那样自动打开它的属性面板。
        /// true = 与原版一致（原版对"砖上只有一个事件"就会自动打开）。
        /// </summary>
        internal const bool AutoOpenOnTileSelect = true;

        /// <summary>
        /// 注册图标。只在 <c>GCS.levelEventIcons</c> 已经被游戏创建之后追加，
        /// 绝不抢先创建该字典（否则 scnEditor 的 LoadLevelEventSprites 会因“非 null”而跳过官方图标）。
        /// </summary>
        internal static void EnsureIcon()
        {
            try
            {
                if (GCS.levelEventIcons == null || GCS.levelEventIcons.ContainsKey(Type))
                {
                    return;
                }

                if (icon == null)
                {
                    icon = CreateIcon();
                }

                GCS.levelEventIcons[Type] = icon;
            }
            catch (Exception e)
            {
                Main.Error("EnsureIcon failed: " + e);
            }
        }

        internal static void Unregister()
        {
            try
            {
                registered = false;

                if (GCS.levelEventsInfo != null)
                {
                    GCS.levelEventsInfo.Remove(DictKey);
                }

                if (GCS.levelEventTypeString != null)
                {
                    GCS.levelEventTypeString.Remove(Type);
                }

                if (GCS.levelEventIcons != null)
                {
                    GCS.levelEventIcons.Remove(Type);
                }
            }
            catch (Exception e)
            {
                Main.Error("Unregister failed: " + e);
            }
        }

        internal static bool IsRegistered
        {
            get { return registered && GCS.levelEventsInfo != null && GCS.levelEventsInfo.ContainsKey(DictKey); }
        }

        // ------------------------------------------------------------------
        // 事件元数据
        // ------------------------------------------------------------------

        private static LevelEventInfo BuildInfo()
        {
            var info = new LevelEventInfo
            {
                name = FileName,
                type = Type,
                pro = false,
                taroDLC = false,
                isDecoration = false,
                useGroups = false,
                // 允许放在第一块砖上：原版在选中首砖时会禁用（并可能整类隐藏）
                // allowFirstFloorCheck 为 false 的按钮。
                allowFirstFloor = true,
                executionTime = LevelEventExecutionTime.OnBar,
                categories = new List<LevelEventCategory> { LevelEventCategory.VisualFx },
                propertiesInfo = new Dictionary<string, PropertyInfo>()
            };

            // 顺序 = 面板里从上到下的行顺序（PropertiesPanel 按字典插入顺序渲染）。
            Add(info, 0, new Dictionary<string, object>
            {
                { "name", "floor" },
                { "type", "Int" },
                { "customLabel", "Tile" }
            });

            Add(info, 1, new Dictionary<string, object>
            {
                { "name", "tintColor" },
                { "type", "Color" },
                { "default", "ffffff" },
                { "usesAlpha", false },
                { "customLabel", "Tint color" },
                { "affectsFloors", true }
            });

            Add(info, 2, new Dictionary<string, object>
            {
                { "name", "tintOpacity" },
                { "type", "Float" },
                { "default", 100f },
                { "min", 0f },
                { "max", 100f },
                { "customLabel", "Opacity (%)" },
                { "affectsFloors", true }
            });

            Add(info, 3, new Dictionary<string, object>
            {
                { "name", "tilesAhead" },
                { "type", "Int" },
                { "default", 0 },
                { "min", 0 },
                { "max", 99 },
                { "customLabel", "Affect next tiles" },
                { "affectsFloors", true }
            });

            return info;
        }

        private static void Add(LevelEventInfo info, int order, Dictionary<string, object> dict)
        {
            var property = new PropertyInfo(dict, info) { order = order };
            info.propertiesInfo[(string)dict["name"]] = property;
        }

        // ------------------------------------------------------------------
        // 图标（运行时生成，免去打包资源）
        // ------------------------------------------------------------------

        private static Sprite CreateIcon()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var border = new Color(0.10f, 0.10f, 0.12f, 1f);
            var fill = new Color(0.98f, 0.62f, 0.20f, 1f);

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x < 3 || y < 3 || x >= size - 3 || y >= size - 3;
                    // 中间画一个斜向色块，让它和其它事件图标区分开
                    bool isAccent = x + y > size && x + y < size * 2 - 4;
                    pixels[y * size + x] = isBorder ? border : (isAccent ? fill : Color.white);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
