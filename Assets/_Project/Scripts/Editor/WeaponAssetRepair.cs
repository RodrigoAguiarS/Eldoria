using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using DungeonCrawler.Systems;
using UnityEditor;
using UnityEngine;

namespace DungeonCrawler.Editor
{
    /// <summary>Repairs only the two diagnosed scriptless assets through AssetDatabase.</summary>
    public static class WeaponAssetRepair
    {
        public const string IronPath = "Assets/_Project/ScriptableObjects/Items/Weapon_IronSword.asset";
        public const string WardenPath = "Assets/_Project/ScriptableObjects/Items/Weapon_WardenBlade.asset";

        [MenuItem("Tools/Dungeon Crawler/Audit/Repair Weapon Assets")]
        public static void Repair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do PlayMode.");
            RepairAsset(IronPath);
            RepairAsset(WardenPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset("Assets/_Project/ScriptableObjects/Loot/Loot_Elite.asset", ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset("Assets/_Project/ScriptableObjects/Loot/Loot_Boss.asset", ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory("Reports/Leadership");
            File.WriteAllText("Reports/Leadership/weapon-repair.txt", "Both weapon assets load. Existing GUIDs and serialized values preserved.\n");
        }

        private static void RepairAsset(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<WeaponData>(path) != null) return;
            string content = File.ReadAllText(path);
            if (!Regex.IsMatch(content, @"m_Script:\s*\{fileID:\s*0\s*\}"))
                throw new InvalidOperationException("Refusing to overwrite an asset with an unknown script state: " + path);
            Match display = Regex.Match(content, @"(?m)^  displayName:\s*(.+)$");
            Match bonus = Regex.Match(content, @"(?m)^  damageBonus:\s*([^\r\n]+)$");
            if (!display.Success || !bonus.Success) throw new InvalidOperationException("Cannot recover original weapon values: " + path);
            string name = display.Groups[1].Value.Trim();
            if (name.StartsWith("\"", StringComparison.Ordinal) && name.EndsWith("\"", StringComparison.Ordinal))
                name = Regex.Unescape(name.Substring(1, name.Length - 2));
            float damage = float.Parse(bonus.Groups[1].Value, CultureInfo.InvariantCulture);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Original GUID is missing: " + path);
            var weapon = ScriptableObject.CreateInstance<WeaponData>();
            weapon.name = Path.GetFileNameWithoutExtension(path);
            weapon.displayName = name;
            weapon.damageBonus = damage;
            // CreateAsset overwrites the diagnosed invalid native data at its existing path.
            // Its .meta is retained; no DeleteAsset or text edits to serialized assets.
            AssetDatabase.CreateAsset(weapon, path);
            AssetDatabase.SaveAssets();
            if (AssetDatabase.AssetPathToGUID(path) != guid) throw new InvalidOperationException("Weapon GUID changed: " + path);
            WeaponData restored = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (restored == null || restored.displayName != name || restored.damageBonus != damage)
                throw new InvalidOperationException("Weapon restoration verification failed: " + path);
        }
    }
}
