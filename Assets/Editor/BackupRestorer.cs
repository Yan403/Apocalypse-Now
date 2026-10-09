using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class BackupRestorer : EditorWindow
{
    private static string backupsRootFolder = @"C:/Users/Yan/Desktop/BACKUP CÓDIGOS";

    // Formatos de backup suportados
    private enum BackupFormat
    {
        LegacyTxt,      // Arquivos _System.txt antigos
        StructuredFolder // Nova estrutura com pastas
    }

    [MenuItem("Tools/Restauracao/Restaurar Backup com Estrutura (Recomendado)")]
    public static void RestoreWithStructure()
    {
        if (!Directory.Exists(backupsRootFolder))
        {
            EditorUtility.DisplayDialog("Erro", $"Pasta de backup nao encontrada:\n{backupsRootFolder}\n\nUse 'Configurar Pasta de Backup' primeiro.", "OK");
            return;
        }

        // Verifica se existe backup estruturado (com arquivos .backup)
        string[] backupFiles = Directory.GetFiles(backupsRootFolder, "*.backup", SearchOption.AllDirectories);

        if (backupFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Aviso",
                "Nenhum backup no formato estruturado encontrado.\n\n" +
                "Deseja tentar restaurar usando o formato TXT antigo?",
                "Sim", "Nao");

            if (EditorUtility.DisplayDialog("Formato Alternativo",
                "Tentar restaurar usando arquivos _System.txt?",
                "Sim", "Nao"))
            {
                RestoreFromLegacyFormat();
            }
            return;
        }

        bool confirm = EditorUtility.DisplayDialog("Confirmar Restauracao",
            $"Encontrados {backupFiles.Length} scripts para restaurar.\n\n" +
            "Isso irá recriar TODAS as pastas e subpastas exatamente como estavam no projeto original.\n\n" +
            "Deseja continuar?",
            "Sim, Restaurar", "Cancelar");

        if (!confirm) return;

        RestoreStructuredBackup();
    }

    [MenuItem("Tools/Restauracao/Restaurar Backup TXT (Legado)")]
    public static void RestoreFromLegacyFormat()
    {
        if (!Directory.Exists(backupsRootFolder))
        {
            EditorUtility.DisplayDialog("Erro", $"Pasta de backup nao encontrada:\n{backupsRootFolder}", "OK");
            return;
        }

        // Procura por arquivos _System.txt
        string[] systemFiles = Directory.GetFiles(backupsRootFolder, "*_System.txt", SearchOption.AllDirectories);

        if (systemFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Aviso",
                "Nenhum arquivo _System.txt encontrado.\n\n" +
                "Use 'Restaurar Backup - Escolher Arquivo' para selecionar manualmente.",
                "OK");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog("Confirmar Restauracao",
            $"Encontrados {systemFiles.Length} arquivos de sistema.\n\n" +
            "ATENCAO: Este formato NAO preserva subpastas!\n" +
            "Todos os scripts serao colocados em pastas simples.\n\n" +
            "Deseja continuar?",
            "Sim", "Cancelar");

        if (!confirm) return;

        int successCount = 0;
        foreach (string file in systemFiles)
        {
            if (RestoreFromLegacyFile(file, false))
                successCount++;
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Restauracao Concluida",
            $"Restaurados {successCount} sistemas.\n\nLocal: Assets/Scripts/",
            "OK");
    }

    [MenuItem("Tools/Restauracao/Restaurar Backup - Escolher Arquivo")]
    public static void RestoreSelectedFile()
    {
        string backupPath = EditorUtility.OpenFilePanel("Selecione o arquivo de backup", backupsRootFolder, "txt");

        if (string.IsNullOrEmpty(backupPath))
        {
            Debug.Log("Restauracao cancelada.");
            return;
        }

        RestoreFromLegacyFile(backupPath, true);
    }

    [MenuItem("Tools/Restauracao/Configurar Pasta de Backup")]
    public static void ConfigureBackupFolder()
    {
        string newPath = EditorUtility.OpenFolderPanel("Selecione a pasta onde estao seus backups",
            backupsRootFolder, "");

        if (!string.IsNullOrEmpty(newPath))
        {
            backupsRootFolder = newPath;
            EditorUtility.DisplayDialog("Configuracao", $"Pasta de backup configurada para:\n{backupsRootFolder}", "OK");
            Debug.Log($"Pasta de backup alterada para: {backupsRootFolder}");
        }
    }

    [MenuItem("Tools/Restauracao/Abrir Pasta de Backup")]
    public static void OpenBackupFolder()
    {
        if (Directory.Exists(backupsRootFolder))
        {
            EditorUtility.RevealInFinder(backupsRootFolder);
        }
        else
        {
            Debug.LogError($"Pasta de backup nao encontrada: {backupsRootFolder}");
        }
    }

    // ============================================================================
    // RESTAURACAO ESTRUTURADA (preserva pastas)
    // ============================================================================

    private static void RestoreStructuredBackup()
    {
        try
        {
            string scriptsPath = Path.Combine(Application.dataPath, "Scripts");

            // Encontra todos os arquivos .backup
            string[] backupFiles = Directory.GetFiles(backupsRootFolder, "*.backup", SearchOption.AllDirectories);

            int restoredCount = 0;
            List<string> createdFolders = new List<string>();

            foreach (string backupFile in backupFiles)
            {
                // Calcula o caminho relativo a partir da pasta de backup
                string relativePath = backupFile.Substring(backupsRootFolder.Length + 1);

                // Remove a extensao .backup para obter o caminho original do script
                string originalScriptPath = relativePath.Replace(".backup", "");

                // Caminho de destino no novo projeto
                string destFilePath = Path.Combine(scriptsPath, originalScriptPath);

                // Cria a pasta de destino se nao existir
                string destDir = Path.GetDirectoryName(destFilePath);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                    createdFolders.Add(destDir);
                }

                // Copia o conteudo do backup
                string content = File.ReadAllText(backupFile);
                File.WriteAllText(destFilePath, content);
                restoredCount++;
            }

            AssetDatabase.Refresh();

            // Mensagem de sucesso
            string folderMessage = createdFolders.Count > 0 ?
                $"\n\nPastas criadas: {createdFolders.Count}" : "";

            EditorUtility.DisplayDialog("Restauracao Concluida",
                $"Restaurados {restoredCount} scripts com estrutura completa!{folderMessage}\n\nLocal: Assets/Scripts/",
                "OK");

            Debug.Log($"Restaurados {restoredCount} scripts mantendo a estrutura de pastas original.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro na restauracao estruturada: {e.Message}");
            EditorUtility.DisplayDialog("Erro", $"Falha na restauracao:\n{e.Message}", "OK");
        }
    }

    // ============================================================================
    // RESTAURACAO LEGADO (arquivos _System.txt)
    // ============================================================================

    private static bool RestoreFromLegacyFile(string backupFilePath, bool showMessages = true)
    {
        try
        {
            if (!File.Exists(backupFilePath))
            {
                if (showMessages) Debug.LogError($"Arquivo nao encontrado: {backupFilePath}");
                return false;
            }

            string content = File.ReadAllText(backupFilePath);
            string fileName = Path.GetFileNameWithoutExtension(backupFilePath);

            // Extrai o nome do sistema
            string systemName = ExtractSystemName(content, fileName);

            if (string.IsNullOrEmpty(systemName))
            {
                if (showMessages)
                {
                    systemName = EditorUtility.SaveFilePanelInProject("Nome da Pasta",
                        fileName.Replace("_System", ""), "", "Digite o nome da pasta para os scripts:");

                    if (string.IsNullOrEmpty(systemName))
                    {
                        Debug.Log("Restauracao cancelada.");
                        return false;
                    }
                }
                else
                {
                    systemName = fileName.Replace("_System", "");
                }
            }

            // Extrai os scripts
            Dictionary<string, string> scripts = ExtractScriptsFromLegacyBackup(content);

            if (scripts.Count == 0)
            {
                if (showMessages)
                    EditorUtility.DisplayDialog("Erro", "Nenhum script encontrado no arquivo.", "OK");
                return false;
            }

            // Define o caminho de destino
            string scriptsBasePath = Path.Combine(Application.dataPath, "Scripts");
            string targetFolder = Path.Combine(scriptsBasePath, systemName);

            if (!Directory.Exists(targetFolder))
                Directory.CreateDirectory(targetFolder);

            // Restaura os scripts
            foreach (var script in scripts)
            {
                string scriptPath = Path.Combine(targetFolder, script.Key);
                File.WriteAllText(scriptPath, script.Value);
            }

            AssetDatabase.Refresh();

            if (showMessages)
            {
                Debug.Log($"Restaurado: {systemName} - {scripts.Count} scripts");
                EditorUtility.DisplayDialog("Restauracao Concluida",
                    $"Sistema: {systemName}\nScripts: {scripts.Count}\nLocal: Assets/Scripts/{systemName}",
                    "OK");
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro ao restaurar: {e.Message}");
            return false;
        }
    }

    private static string ExtractSystemName(string content, string fileName)
    {
        Match match = Regex.Match(content, @"//\s*SISTEMA:\s*([\w\d]+)");
        if (match.Success)
            return match.Groups[1].Value;

        match = Regex.Match(content, @"SISTEMA:\s*([\w\d]+)", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value;

        string cleanName = fileName
            .Replace("_System", "")
            .Replace("_BACKUP", "")
            .Replace("_AllInOne", "")
            .Replace("_SCRIPTS", "");

        return !string.IsNullOrEmpty(cleanName) ? cleanName : null;
    }

    private static Dictionary<string, string> ExtractScriptsFromLegacyBackup(string content)
    {
        Dictionary<string, string> scripts = new Dictionary<string, string>();

        // Metodo 1: Padrao completo
        string pattern = @"//\s*([\w\d]+\.cs)\s*-\s*TAMANHO:.*?\n//\s*-+\s*\n([\s\S]*?)(?=\n//\s*-+\s*\n//\s*[\w\d]+\.cs|$)";
        var matches = Regex.Matches(content, pattern, RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            string scriptName = match.Groups[1].Value.Trim();
            string scriptContent = match.Groups[2].Value.Trim();
            scriptContent = CleanScriptContent(scriptContent);

            if (!string.IsNullOrEmpty(scriptContent))
                scripts[scriptName] = scriptContent;
        }

        // Metodo 2: Padrao simples
        if (scripts.Count == 0)
        {
            string simplePattern = @"//\s*-+\s*\n//\s*([\w\d]+\.cs).*?\n//\s*-+\s*\n([\s\S]*?)(?=\n//\s*-+\s*\n//|$)";
            var simpleMatches = Regex.Matches(content, simplePattern, RegexOptions.Singleline);

            foreach (Match match in simpleMatches)
            {
                string scriptName = match.Groups[1].Value.Trim();
                string scriptContent = match.Groups[2].Value.Trim();
                scriptContent = CleanScriptContent(scriptContent);

                if (!string.IsNullOrEmpty(scriptContent))
                    scripts[scriptName] = scriptContent;
            }
        }

        return scripts;
    }

    private static string CleanScriptContent(string content)
    {
        string[] lines = content.Split('\n');
        List<string> cleanedLines = new List<string>();

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed) &&
                !trimmed.StartsWith("// ----") &&
                !trimmed.StartsWith("// ===") &&
                !trimmed.StartsWith("// SCRIPT") &&
                !trimmed.StartsWith("// CAMINHO") &&
                !trimmed.StartsWith("// TAMANHO"))
            {
                cleanedLines.Add(line);
            }
        }

        return string.Join("\n", cleanedLines).Trim();
    }
}