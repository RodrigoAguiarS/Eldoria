using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>Bounded JSON IO, durable temp and same-volume atomic replacement.</summary>
    internal sealed class RunCheckpointFile
    {
        private const int MaxBytes = 1024 * 1024;
        private readonly string path;
        public RunCheckpointFile(string path) => this.path = path;

        public static bool TryRead(string path, RunCheckpointCatalog catalog, out RunCheckpointData data, out string message)
        {
            data = null; message = "Checkpoint inválido ou incompatível.";
            try
            {
                if (!File.Exists(path) || new FileInfo(path).Length > MaxBytes) return false;
                string json = File.ReadAllText(path).Trim();
                if (!json.StartsWith("{", StringComparison.Ordinal) || !json.EndsWith("}", StringComparison.Ordinal)) return false;
                var parsed = JsonUtility.FromJson<RunCheckpointData>(json);
                if (catalog == null || !catalog.Validate(parsed, out message)) return false;
                data = parsed; return true;
            }
            catch (Exception e) when (IsFileError(e)) { message = "Não foi possível ler o checkpoint."; return false; }
        }

        public bool TrySave(RunCheckpointData data, RunCheckpointCatalog catalog, out string message)
        {
            if (!catalog.Validate(data, out message)) return false;
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
                if (bytes.Length > MaxBytes) { message = "Checkpoint excede o limite do arquivo."; return false; }
                using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (File.Exists(path))
                {
                    // A corrupt primary must never replace a usable backup.
                    if (TryRead(path, catalog, out _, out _)) File.Replace(path + ".tmp", path, path + ".bak");
                    else File.Replace(path + ".tmp", path, null);
                }
                else File.Move(path + ".tmp", path);
                message = "Checkpoint salvo · retome pelo menu."; return true;
            }
            catch (Exception e) when (IsFileError(e))
            { message = "Checkpoint não salvo. Verifique o espaço e o acesso ao disco."; return false; }
        }

        public bool TryDelete(out string message)
        {
            try
            {
                File.Delete(path + ".bak"); File.Delete(path + ".tmp"); File.Delete(path);
                message = ""; return true;
            }
            catch (Exception e) when (IsFileError(e))
            { message = "Não foi possível descartar o checkpoint anterior."; return false; }
        }

        private static bool IsFileError(Exception e) => e is IOException || e is UnauthorizedAccessException ||
            e is ArgumentException || e is NotSupportedException || e is System.Security.SecurityException;
    }
}
