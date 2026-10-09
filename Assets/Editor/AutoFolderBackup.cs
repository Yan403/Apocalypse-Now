using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class AutoFolderBackup : AssetModificationProcessor
{
    // Pasta raiz onde os backups serão salvos
    private static string backupsRootFolder = @"C:/Users/Yan/Desktop/BACKUP CÓDIGOS";

    // Cache das pastas e seus arquivos de destino
    private static Dictionary<string, string> folderBackupMap = null;
    private static Dictionary<string, System.DateTime> lastBackupTime = new Dictionary<string, System.DateTime>();

    // --- ALL-IN-ONE ---
    private const string ALL_IN_ONE_FILENAME = "BACKUP_AllInOne_SCRIPTS.txt";

    private static void BuildFolderMap()
    {
        folderBackupMap = new Dictionary<string, string>();

        string absoluteScriptsPath = Path.Combine(Application.dataPath, "Scripts");

        if (!Directory.Exists(absoluteScriptsPath))
        {
            Debug.LogError($"Pasta nao encontrada: {absoluteScriptsPath}");
            return;
        }

        string[] allFolders = Directory.GetDirectories(absoluteScriptsPath, "*", SearchOption.AllDirectories);

        // Scripts na raiz de Scripts
        string[] rootScripts = Directory.GetFiles(absoluteScriptsPath, "*.cs", SearchOption.TopDirectoryOnly);
        if (rootScripts.Length > 0)
        {
            string folderName = Path.GetFileName(absoluteScriptsPath);
            string backupFilePath = Path.Combine(backupsRootFolder, $"{folderName}_System.txt");
            folderBackupMap[absoluteScriptsPath] = backupFilePath;
        }

        foreach (string folderPath in allFolders)
        {
            string[] scripts = Directory.GetFiles(folderPath, "*.cs", SearchOption.TopDirectoryOnly);
            if (scripts.Length > 0)
            {
                string folderName = Path.GetFileName(folderPath);
                string relativePath = folderPath.Replace(absoluteScriptsPath + Path.DirectorySeparatorChar, "");
                string backupFolder = Path.Combine(backupsRootFolder, relativePath);
                string backupFilePath = Path.Combine(backupFolder, $"{folderName}_System.txt");

                folderBackupMap[folderPath] = backupFilePath;
            }
        }

        Debug.Log($"{folderBackupMap.Count} pastas de sistemas detectadas");
    }

    private static string[] OnWillSaveAssets(string[] paths)
    {
        if (folderBackupMap == null)
            BuildFolderMap();

        if (folderBackupMap == null || folderBackupMap.Count == 0)
            return paths;

        HashSet<string> affectedFolders = new HashSet<string>();

        foreach (string savedPath in paths)
        {
            if (!savedPath.EndsWith(".cs"))
                continue;

            string absoluteSavedPath = Path.Combine(Application.dataPath, savedPath.Replace("Assets/", ""));
            absoluteSavedPath = absoluteSavedPath.Replace("/", Path.DirectorySeparatorChar.ToString());

            // Backup individual (.backup)
            BackupSingleScript(absoluteSavedPath);

            foreach (string folderPath in folderBackupMap.Keys)
            {
                if (absoluteSavedPath.StartsWith(folderPath + Path.DirectorySeparatorChar) ||
                    Path.GetDirectoryName(absoluteSavedPath) == folderPath)
                {
                    affectedFolders.Add(folderPath);
                    break;
                }
            }
        }

        foreach (string folderPath in affectedFolders)
        {
            if (ShouldBackupFolder(folderPath))
            {
                BackupFolderContents(folderPath, folderBackupMap[folderPath]);
                lastBackupTime[folderPath] = System.DateTime.Now;
            }
        }

        // --- ALL-IN-ONE NÃO é atualizado a cada save automático (evita lentidão) ---
        // Use o menu "Tools/Backup" para gerá-lo quando desejar.

        return paths;
    }

    private static bool ShouldBackupFolder(string folderPath)
    {
        if (!lastBackupTime.ContainsKey(folderPath))
            return true;
        return (System.DateTime.Now - lastBackupTime[folderPath]).TotalSeconds >= 2;
    }

    private static void BackupSingleScript(string scriptPath)
    {
        try
        {
            string relativePath = scriptPath.Substring(Application.dataPath.Length + 1);
            string backupFilePath = Path.Combine(backupsRootFolder, relativePath + ".backup");

            string backupDir = Path.GetDirectoryName(backupFilePath);
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            string content = File.ReadAllText(scriptPath);
            File.WriteAllText(backupFilePath, content);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro no backup incremental de {scriptPath}: {e.Message}");
        }
    }

    private static void BackupFolderContents(string sourceFolder, string outputTxtPath)
    {
        try
        {
            string[] scriptFiles = Directory.GetFiles(sourceFolder, "*.cs", SearchOption.TopDirectoryOnly);
            if (scriptFiles.Length == 0) return;

            string directory = Path.GetDirectoryName(outputTxtPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            StringBuilder combinedContent = new StringBuilder();
            long totalSizeBytes = 0;
            Dictionary<string, long> fileSizes = new Dictionary<string, long>();

            foreach (string scriptPath in scriptFiles)
            {
                FileInfo fileInfo = new FileInfo(scriptPath);
                fileSizes[scriptPath] = fileInfo.Length;
                totalSizeBytes += fileInfo.Length;
            }

            string totalSizeFormatted = FormatFileSize(totalSizeBytes);
            string systemName = Path.GetFileName(sourceFolder);

            combinedContent.AppendLine("// ============================================================================");
            combinedContent.AppendLine($"// SISTEMA: {systemName}");
            combinedContent.AppendLine($"// DATA: {System.DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            combinedContent.AppendLine($"// SCRIPTS: {scriptFiles.Length}");
            combinedContent.AppendLine($"// TAMANHO TOTAL: {totalSizeFormatted} ({totalSizeBytes} bytes)");
            combinedContent.AppendLine("// ============================================================================");
            combinedContent.AppendLine();

            foreach (string scriptPath in scriptFiles.OrderBy(x => Path.GetFileName(x)))
            {
                string scriptName = Path.GetFileName(scriptPath);
                string scriptContent = File.ReadAllText(scriptPath);
                string fileSizeFormatted = FormatFileSize(fileSizes[scriptPath]);

                combinedContent.AppendLine("// ------------------------------------------------------------------------");
                combinedContent.AppendLine($"// {scriptName} - TAMANHO: {fileSizeFormatted} ({fileSizes[scriptPath]} bytes)");
                combinedContent.AppendLine("// ------------------------------------------------------------------------");
                combinedContent.AppendLine();
                combinedContent.AppendLine(scriptContent);
                combinedContent.AppendLine();
                combinedContent.AppendLine();
            }

            combinedContent.AppendLine("// ============================================================================");
            combinedContent.AppendLine($"// FIM - {systemName}");
            combinedContent.AppendLine($"// TAMANHO TOTAL: {totalSizeFormatted} ({totalSizeBytes} bytes)");
            combinedContent.AppendLine("// ============================================================================");

            File.WriteAllText(outputTxtPath, combinedContent.ToString());
            Debug.Log($"{systemName}: {scriptFiles.Length} scripts - Total: {totalSizeFormatted}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro em {Path.GetFileName(sourceFolder)}: {e.Message}");
        }
    }

    // --- ALL-IN-ONE: Gera um único TXT com todos os scripts do projeto ---
    private static void BackupAllInOne()
    {
        try
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            if (!Directory.Exists(scriptsRoot))
            {
                Debug.LogError("Pasta Assets/Scripts não encontrada.");
                return;
            }

            // Coleta todos os arquivos .cs em todas as subpastas
            string[] allScripts = Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories);

            if (allScripts.Length == 0)
            {
                Debug.LogWarning("Nenhum script encontrado para o All-in-One.");
                return;
            }

            // Ordena por caminho para consistência
            var sortedScripts = allScripts.OrderBy(p => p).ToList();

            StringBuilder sb = new StringBuilder();
            long totalSizeBytes = 0;

            // Cabeçalho
            sb.AppendLine("==================================================================================");
            sb.AppendLine("                    BACKUP COMPLETO - TODOS OS SCRIPTS DO PROJETO");
            sb.AppendLine("==================================================================================");
            sb.AppendLine($"DATA/HORA: {System.DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"TOTAL DE SCRIPTS: {sortedScripts.Count}");

            // Calcula tamanho total
            foreach (string script in sortedScripts)
            {
                FileInfo fi = new FileInfo(script);
                totalSizeBytes += fi.Length;
            }
            string totalSizeFormatted = FormatFileSize(totalSizeBytes);
            sb.AppendLine($"TAMANHO TOTAL: {totalSizeFormatted} ({totalSizeBytes} bytes)");
            sb.AppendLine($"PROJETO: {Application.productName}");
            sb.AppendLine("==================================================================================");
            sb.AppendLine();
            sb.AppendLine();

            // Agrupa por sistema (pasta relativa a Scripts)
            string[] directories = Directory.GetDirectories(scriptsRoot, "*", SearchOption.AllDirectories);
            var folders = new List<string> { scriptsRoot }; // Raiz também entra
            folders.AddRange(directories);

            foreach (string folder in folders.OrderBy(f => f))
            {
                // Pega scripts apenas dessa pasta (sem subpastas)
                string[] scriptsInFolder = Directory.GetFiles(folder, "*.cs", SearchOption.TopDirectoryOnly);
                if (scriptsInFolder.Length == 0) continue;

                string systemName = Path.GetFileName(folder);
                if (systemName == "Scripts") systemName = "Scripts (Raiz)";

                sb.AppendLine("+-------------------------------------------------------------------------------+");
                sb.AppendLine($"| SISTEMA: {folder.Replace(scriptsRoot, "Scripts")}");
                sb.AppendLine("+-------------------------------------------------------------------------------+");
                sb.AppendLine($"| SCRIPTS: {scriptsInFolder.Length}");

                long folderSize = 0;
                foreach (string s in scriptsInFolder)
                    folderSize += new FileInfo(s).Length;

                sb.AppendLine($"| TAMANHO TOTAL: {FormatFileSize(folderSize)} ({folderSize} bytes)");
                sb.AppendLine("+-------------------------------------------------------------------------------+");
                sb.AppendLine();

                int count = 0;
                foreach (string scriptPath in scriptsInFolder.OrderBy(Path.GetFileName))
                {
                    count++;
                    string scriptName = Path.GetFileName(scriptPath);
                    FileInfo fi = new FileInfo(scriptPath);
                    string content = File.ReadAllText(scriptPath);

                    sb.AppendLine("+=========================================================================+");
                    sb.AppendLine($"| SCRIPT #{sortedScripts.IndexOf(scriptPath) + 1}: {scriptName}");
                    sb.AppendLine($"| TAMANHO: {FormatFileSize(fi.Length)} ({fi.Length} bytes)");
                    sb.AppendLine("+=========================================================================+");
                    sb.AppendLine();
                    sb.AppendLine(content);
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine("----------------------------------------------------------------------------------");
                    sb.AppendLine();
                }
            }

            sb.AppendLine();
            sb.AppendLine("==================================================================================");
            sb.AppendLine($"FIM DO BACKUP COMPLETO - {sortedScripts.Count} scripts exportados");
            sb.AppendLine($"TAMANHO TOTAL: {totalSizeFormatted} ({totalSizeBytes} bytes)");
            sb.AppendLine($"GERADO EM: {System.DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine("==================================================================================");

            // Salva o arquivo
            string outputPath = Path.Combine(backupsRootFolder, ALL_IN_ONE_FILENAME);
            File.WriteAllText(outputPath, sb.ToString());
            Debug.Log($"All-in-One backup salvo em: {outputPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro ao gerar All-in-One: {e.Message}");
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    // --- MENU ITEMS ---
    [MenuItem("Tools/Backup/Forcar Backup de Todos os Sistemas")]
    public static void ForceAllBackups()
    {
        if (folderBackupMap == null)
            BuildFolderMap();

        if (folderBackupMap == null || folderBackupMap.Count == 0)
        {
            EditorUtility.DisplayDialog("Erro",
                "Nenhuma pasta de scripts encontrada!\n\n" +
                "Certifique-se de que existe a pasta: Assets/Scripts",
                "OK");
            return;
        }

        // Backup de todos os scripts individuais (.backup)
        string scriptsPath = Path.Combine(Application.dataPath, "Scripts");
        if (Directory.Exists(scriptsPath))
        {
            string[] allScripts = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);
            foreach (string script in allScripts)
            {
                BackupSingleScript(script);
            }
            Debug.Log($"Backup estrutural: {allScripts.Length} scripts salvos como .backup");
        }

        // Backup dos arquivos _System.txt (agrupados por pasta)
        foreach (var kvp in folderBackupMap)
        {
            BackupFolderContents(kvp.Key, kvp.Value);
        }

        // --- ALL-IN-ONE ---
        BackupAllInOne();

        EditorUtility.DisplayDialog("Backup Concluido",
            "Backups atualizados!\n\n" +
            "- Backup estrutural (.backup): scripts individuais com pastas\n" +
            "- Backup agrupado (_System.txt): por sistema\n" +
            "- Backup All-in-One (BACKUP_AllInOne_SCRIPTS.txt): todos os scripts\n\n" +
            $"Local: {backupsRootFolder}",
            "OK");
    }

    [MenuItem("Tools/Backup/Atualizar All-in-One (apenas)")]
    public static void UpdateAllInOne()
    {
        BackupAllInOne();
        EditorUtility.DisplayDialog("All-in-One",
            "BACKUP_AllInOne_SCRIPTS.txt atualizado com sucesso!",
            "OK");
    }

    [MenuItem("Tools/Backup/Reconstruir Mapeamento")]
    public static void RebuildMap()
    {
        folderBackupMap = null;
        lastBackupTime.Clear();
        BuildFolderMap();
        Debug.Log("Mapeamento reconstruido");
    }
}