
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public static partial class ClassExpansionAssets
{
    private static readonly string[] RequiredUIText =
    {
        "最大HP上昇",
        "魔法攻撃上昇",
        "防御上昇",
        "攻撃力上昇",
        "最大MP上昇",
        "生命吸収",
        "集中攻撃",
        "精密射撃",
        "戦士",
        "魔法使い",
        "バーサーカー",
        "前へ",
        "次へ",
        "閉じる",
        "スキル"
    };

    public static void ValidateExpansionFont()
    {
        string fontPath = SkillFolder + "/Phase3UI.asset";

        TMP_FontAsset font =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);

        if (font == null)
        {
            Debug.LogError($"フォントが見つかりません: {fontPath}");
            return;
        }

        string skillText = string.Concat(
            AssetDatabase.FindAssets("t:SkillData")
                .Select(guid =>
                    AssetDatabase.LoadAssetAtPath<SkillData>(
                        AssetDatabase.GUIDToAssetPath(guid)
                    )
                )
                .Where(skill => skill != null)
                .Select(skill =>
                    (skill.skillName ?? "") +
                    (skill.description ?? "")
                )
        );

        string requiredText =
            skillText + string.Concat(RequiredUIText);

        char[] missingCharacters = requiredText
            .Where(character => !char.IsControl(character))
            .Distinct()
            .Where(character => !font.HasCharacter(character))
            .ToArray();

        if (missingCharacters.Length == 0)
        {
            Debug.Log("Phase3UIの文字確認が完了しました。");
            return;
        }

        Debug.LogWarning(
            "Phase3UIに不足している文字があります: " +
            new string(missingCharacters)
        );
    }

    public static void FixExpansionUI()
    {
        ValidateExpansionFont();

        Debug.Log(
            "UIの配置とサイズはUnity Inspectorで管理します。"
        );
    }
}
