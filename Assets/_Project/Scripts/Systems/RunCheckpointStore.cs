namespace DungeonCrawler.Systems
{
    /// <summary>Wraps primary/backup selection; all candidates pass the same validator.</summary>
    public sealed class RunCheckpointStore
    {
        public string Path { get; }
        public RunCheckpointStore(string path) => Path = path;
        public bool TryLoad(RunCheckpointCatalog catalog, out RunCheckpointData data, out string message)
        {
            if (RunCheckpointFile.TryRead(Path, catalog, out data, out message)) { message = "Checkpoint pronto para continuar."; return true; }
            string primaryError = message;
            if (RunCheckpointFile.TryRead(Path + ".bak", catalog, out data, out _))
            { message = "Backup recuperado · a última gravação não estava disponível."; return true; }
            message = System.IO.File.Exists(Path) || System.IO.File.Exists(Path + ".bak")
                ? primaryError : "Sem checkpoint. Limpe uma sala para salvar.";
            return false;
        }
        public bool TrySave(RunCheckpointData data, RunCheckpointCatalog catalog, out string message) =>
            new RunCheckpointFile(Path).TrySave(data, catalog, out message);
        public bool TryDelete(out string message) => new RunCheckpointFile(Path).TryDelete(out message);
    }
}
