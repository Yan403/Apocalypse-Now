using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

public class AnimatorBackupTool : EditorWindow
{
    private const string BACKUP_FOLDER_PREF = "AnimatorBackup_BackupPath";

    private static string backupRootFolder
    {
        get
        {
            string savedPath = EditorPrefs.GetString(BACKUP_FOLDER_PREF, "");
            if (string.IsNullOrEmpty(savedPath))
            {
                savedPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "ANIMATOR_BACKUPS");
            }
            return savedPath;
        }
        set
        {
            EditorPrefs.SetString(BACKUP_FOLDER_PREF, value);
        }
    }

    private Vector2 scrollPosition;

    [MenuItem("Tools/Animator Backup/Backup Animator Controller")]
    public static void BackupAnimator()
    {
        AnimatorController controller = Selection.activeObject as AnimatorController;

        if (controller == null)
        {
            string path = EditorUtility.OpenFilePanel("Selecione o Animator Controller", "Assets", "controller");
            if (!string.IsNullOrEmpty(path))
            {
                string relativePath = GetRelativePath(path);
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(relativePath);
            }
        }

        if (controller == null)
        {
            EditorUtility.DisplayDialog("Erro", "Selecione um Animator Controller no Project Window primeiro.", "OK");
            return;
        }

        ExportAnimatorController(controller);
    }

    [MenuItem("Tools/Animator Backup/Restaurar Animator Controller")]
    public static void RestoreAnimator()
    {
        string backupPath = EditorUtility.OpenFilePanel("Selecione o arquivo de backup", backupRootFolder, "animator_backup");

        if (string.IsNullOrEmpty(backupPath))
        {
            Debug.Log("Restauracao cancelada.");
            return;
        }

        ImportAnimatorController(backupPath);
    }

    [MenuItem("Tools/Animator Backup/Configurar Pasta de Backup")]
    public static void ConfigureBackupFolder()
    {
        string newPath = EditorUtility.OpenFolderPanel("Selecione a pasta para salvar os backups", backupRootFolder, "");
        if (!string.IsNullOrEmpty(newPath))
        {
            backupRootFolder = newPath;
            EditorUtility.DisplayDialog("Configuracao", $"Pasta de backup configurada para:\n{newPath}", "OK");
        }
    }

    [MenuItem("Tools/Animator Backup/Abrir Pasta de Backup")]
    public static void OpenBackupFolder()
    {
        if (Directory.Exists(backupRootFolder))
        {
            EditorUtility.RevealInFinder(backupRootFolder);
        }
        else
        {
            Directory.CreateDirectory(backupRootFolder);
            EditorUtility.RevealInFinder(backupRootFolder);
        }
    }

    [MenuItem("Tools/Animator Backup/Batch Backup - Todos os Animators")]
    public static void BatchBackupAllAnimators()
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimatorController");

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Aviso", "Nenhum Animator Controller encontrado no projeto.", "OK");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog("Backup em Lote",
            $"Encontrados {guids.Length} Animator Controllers.\n\n" +
            $"Deseja fazer backup de todos eles?\n\nPasta de destino: {backupRootFolder}",
            "Sim", "Nao");

        if (!confirm) return;

        int successCount = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

            if (ExportAnimatorController(controller, false))
            {
                successCount++;
            }
        }

        EditorUtility.DisplayDialog("Backup em Lote Concluido",
            $"Backup concluido!\n\n" +
            $"Sucesso: {successCount}/{guids.Length}\n" +
            $"Local: {backupRootFolder}",
            "OK");
    }

    // ============================================================================
    // EXPORTACAO
    // ============================================================================

    private static bool ExportAnimatorController(AnimatorController controller, bool showMessages = true)
    {
        try
        {
            if (!Directory.Exists(backupRootFolder))
                Directory.CreateDirectory(backupRootFolder);

            string controllerName = controller.name;
            string backupFileName = $"{controllerName}_animator_backup.animator_backup";
            string backupFilePath = Path.Combine(backupRootFolder, backupFileName);

            // Salva descricao do layout
            SaveAnimatorLayoutDescription(controller, backupRootFolder);

            AnimatorBackupData backupData = new AnimatorBackupData();
            backupData.controllerName = controllerName;
            backupData.controllerPath = AssetDatabase.GetAssetPath(controller);
            backupData.backupDate = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            backupData.unityVersion = Application.unityVersion;

            backupData.parameters = ExportParameters(controller);
            backupData.layers = ExportLayers(controller);

            string json = JsonUtility.ToJson(backupData, true);

            StringBuilder finalContent = new StringBuilder();
            finalContent.AppendLine("# ============================================================================");
            finalContent.AppendLine("# ANIMATOR CONTROLLER BACKUP");
            finalContent.AppendLine("# ============================================================================");
            finalContent.AppendLine($"# Nome: {controllerName}");
            finalContent.AppendLine($"# Data: {backupData.backupDate}");
            finalContent.AppendLine($"# Unity: {backupData.unityVersion}");
            finalContent.AppendLine("# ============================================================================");
            finalContent.AppendLine();
            finalContent.AppendLine(json);

            File.WriteAllText(backupFilePath, finalContent.ToString());

            if (showMessages)
            {
                EditorUtility.DisplayDialog("Backup Concluido",
                    $"Animator Controller '{controllerName}' exportado com sucesso!\n\n" +
                    $"Arquivo: {backupFileName}\n\n" +
                    $"Local: {backupRootFolder}",
                    "OK");
            }

            Debug.Log($"Animator exportado: {controllerName} -> {backupFilePath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro ao exportar {controller.name}: {e.Message}");
            if (showMessages)
                EditorUtility.DisplayDialog("Erro", $"Falha na exportacao:\n{e.Message}", "OK");
            return false;
        }
    }

    private static List<AnimatorParameterData> ExportParameters(AnimatorController controller)
    {
        List<AnimatorParameterData> parameters = new List<AnimatorParameterData>();

        foreach (var param in controller.parameters)
        {
            AnimatorParameterData data = new AnimatorParameterData();
            data.name = param.name;
            data.type = param.type.ToString();
            data.defaultValue = GetParameterDefaultValue(param);
            parameters.Add(data);
        }

        return parameters;
    }

    private static object GetParameterDefaultValue(AnimatorControllerParameter param)
    {
        switch (param.type)
        {
            case AnimatorControllerParameterType.Float:
                return param.defaultFloat;
            case AnimatorControllerParameterType.Int:
                return param.defaultInt;
            case AnimatorControllerParameterType.Bool:
                return param.defaultBool;
            case AnimatorControllerParameterType.Trigger:
                return false;
            default:
                return null;
        }
    }

    private static List<AnimatorLayerData> ExportLayers(AnimatorController controller)
    {
        List<AnimatorLayerData> layers = new List<AnimatorLayerData>();

        for (int i = 0; i < controller.layers.Length; i++)
        {
            AnimatorControllerLayer layer = controller.layers[i];
            AnimatorLayerData layerData = new AnimatorLayerData();
            layerData.name = layer.name;
            layerData.weight = layer.defaultWeight;
            layerData.blendingMode = layer.blendingMode.ToString();

            layerData.states = ExportStates(layer.stateMachine);

            layers.Add(layerData);
        }

        return layers;
    }

    private static List<AnimatorStateData> ExportStates(AnimatorStateMachine stateMachine)
    {
        List<AnimatorStateData> states = new List<AnimatorStateData>();

        // Estados normais
        foreach (var state in stateMachine.states)
        {
            AnimatorStateData stateData = new AnimatorStateData();
            stateData.name = state.state.name;
            stateData.type = "State";
            stateData.position = new Vector2Data(state.position.x, state.position.y);

            if (state.state.motion != null)
            {
                stateData.motionName = state.state.motion.name;
                stateData.motionType = GetMotionType(state.state.motion);

                if (state.state.motion is BlendTree blendTree)
                {
                    stateData.blendTreeData = ExportBlendTree(blendTree);
                }
            }

            stateData.speed = state.state.speed;
            stateData.mirror = state.state.mirror;
            stateData.cycleOffset = state.state.cycleOffset;
            stateData.writeDefaultValues = state.state.writeDefaultValues;

            stateData.transitions = ExportTransitions(state.state.transitions);

            states.Add(stateData);
        }

        // Sub-state machines
        foreach (var subStateMachine in stateMachine.stateMachines)
        {
            AnimatorStateData stateData = new AnimatorStateData();
            stateData.name = subStateMachine.stateMachine.name;
            stateData.type = "SubStateMachine";
            stateData.position = new Vector2Data(subStateMachine.position.x, subStateMachine.position.y);
            stateData.subStates = ExportStates(subStateMachine.stateMachine);

            states.Add(stateData);
        }

        // Estado inicial (Entry)
        if (stateMachine.defaultState != null)
        {
            AnimatorStateData entryState = new AnimatorStateData();
            entryState.name = "Entry";
            entryState.type = "Entry";
            entryState.defaultStateName = stateMachine.defaultState.name;
            states.Add(entryState);
        }

        // Any State transitions
        foreach (var transition in stateMachine.anyStateTransitions)
        {
            AnimatorStateData anyStateTransition = new AnimatorStateData();
            anyStateTransition.name = "AnyStateTransition";
            anyStateTransition.type = "AnyState";
            anyStateTransition.transitions = ExportTransitions(new[] { transition });
            states.Add(anyStateTransition);
        }

        return states;
    }

    private static string GetMotionType(Motion motion)
    {
        if (motion is AnimationClip)
            return "AnimationClip";
        if (motion is BlendTree)
            return "BlendTree";
        return "Unknown";
    }

    private static BlendTreeData ExportBlendTree(BlendTree blendTree)
    {
        BlendTreeData data = new BlendTreeData();
        data.name = blendTree.name;
        data.blendType = blendTree.blendType.ToString();
        data.blendParameter = blendTree.blendParameter;
        data.blendParameterY = blendTree.blendParameterY;

        data.children = new List<BlendTreeChildData>();
        for (int i = 0; i < blendTree.children.Length; i++)
        {
            BlendTreeChildData child = new BlendTreeChildData();
            var childMotion = blendTree.children[i];

            child.motionName = childMotion.motion != null ? childMotion.motion.name : "None";
            child.threshold = childMotion.threshold;
            child.position = new Vector2Data(childMotion.position.x, childMotion.position.y);
            child.timeScale = childMotion.timeScale;
            child.cycleOffset = childMotion.cycleOffset;

            if (childMotion.motion is BlendTree childBlendTree)
            {
                child.blendTreeData = ExportBlendTree(childBlendTree);
            }

            data.children.Add(child);
        }

        return data;
    }

    private static List<AnimatorTransitionData> ExportTransitions(AnimatorStateTransition[] transitions)
    {
        List<AnimatorTransitionData> transitionList = new List<AnimatorTransitionData>();

        foreach (var transition in transitions)
        {
            AnimatorTransitionData data = new AnimatorTransitionData();
            data.destinationState = transition.destinationState != null ? transition.destinationState.name : "";
            data.hasExitTime = transition.hasExitTime;
            data.exitTime = transition.exitTime;
            data.duration = transition.duration;
            data.offset = transition.offset;
            data.transitionDuration = transition.duration;
            data.canTransitionToSelf = transition.canTransitionToSelf;

            data.conditions = new List<AnimatorConditionData>();
            foreach (var condition in transition.conditions)
            {
                AnimatorConditionData conditionData = new AnimatorConditionData();
                conditionData.parameter = condition.parameter;
                conditionData.mode = condition.mode.ToString();
                conditionData.threshold = condition.threshold;
                data.conditions.Add(conditionData);
            }

            transitionList.Add(data);
        }

        return transitionList;
    }

    private static void SaveAnimatorLayoutDescription(AnimatorController controller, string outputFolder)
    {
        string layoutPath = Path.Combine(outputFolder, $"{controller.name}_animator_layout.txt");
        StringBuilder layout = new StringBuilder();

        layout.AppendLine("# ANIMATOR CONTROLLER LAYOUT DESCRIPTION");
        layout.AppendLine($"# Controller: {controller.name}");
        layout.AppendLine($"# Generated: {System.DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        layout.AppendLine();
        layout.AppendLine("## STATE POSITIONS");
        layout.AppendLine();

        foreach (var layer in controller.layers)
        {
            layout.AppendLine($"### Layer: {layer.name}");
            ExportStatePositions(layer.stateMachine, layout, 1);
        }

        File.WriteAllText(layoutPath, layout.ToString());
        Debug.Log($"Layout description saved: {layoutPath}");
    }

    private static void ExportStatePositions(AnimatorStateMachine stateMachine, StringBuilder layout, int indent)
    {
        string indentStr = new string(' ', indent * 2);

        foreach (var state in stateMachine.states)
        {
            layout.AppendLine($"{indentStr}- State: {state.state.name} at position ({state.position.x}, {state.position.y})");
        }

        foreach (var subMachine in stateMachine.stateMachines)
        {
            layout.AppendLine($"{indentStr}- SubStateMachine: {subMachine.stateMachine.name} at position ({subMachine.position.x}, {subMachine.position.y})");
            ExportStatePositions(subMachine.stateMachine, layout, indent + 1);
        }
    }

    // ============================================================================
    // IMPORTACAO
    // ============================================================================

    private static void ImportAnimatorController(string backupFilePath)
    {
        try
        {
            string content = File.ReadAllText(backupFilePath);

            int jsonStart = content.IndexOf('{');
            if (jsonStart < 0)
            {
                EditorUtility.DisplayDialog("Erro", "Arquivo de backup invalido.", "OK");
                return;
            }

            string json = content.Substring(jsonStart);
            AnimatorBackupData backupData = JsonUtility.FromJson<AnimatorBackupData>(json);

            if (backupData == null)
            {
                EditorUtility.DisplayDialog("Erro", "Falha ao ler o arquivo de backup.", "OK");
                return;
            }

            string savePath = EditorUtility.SaveFilePanelInProject("Salvar Animator Controller",
                backupData.controllerName, "controller", "Escolha onde salvar o Animator Controller");

            if (string.IsNullOrEmpty(savePath))
            {
                Debug.Log("Restauracao cancelada.");
                return;
            }

            AnimatorController newController = AnimatorController.CreateAnimatorControllerAtPath(savePath);
            newController.name = backupData.controllerName;

            // Remove o estado padrao criado automaticamente
            if (newController.layers.Length > 0 && newController.layers[0].stateMachine.states.Length > 0)
            {
                var defaultState = newController.layers[0].stateMachine.states[0];
                newController.layers[0].stateMachine.RemoveState(defaultState.state);
            }

            RestoreParameters(newController, backupData.parameters);
            RestoreLayers(newController, backupData.layers);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Restauracao Concluida",
                $"Animator Controller '{backupData.controllerName}' restaurado com sucesso!\n\n" +
                $"Local: {savePath}",
                "OK");

            Selection.activeObject = newController;
            EditorGUIUtility.PingObject(newController);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro ao restaurar: {e.Message}");
            EditorUtility.DisplayDialog("Erro", $"Falha na restauracao:\n{e.Message}", "OK");
        }
    }

    private static void RestoreParameters(AnimatorController controller, List<AnimatorParameterData> parameters)
    {
        foreach (var paramData in parameters)
        {
            AnimatorControllerParameterType paramType = AnimatorControllerParameterType.Float;
            System.Enum.TryParse(paramData.type, out paramType);

            controller.AddParameter(paramData.name, paramType);

            var param = controller.parameters.FirstOrDefault(p => p.name == paramData.name);
            if (param != null)
            {
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Float:
                        param.defaultFloat = (float)paramData.defaultValue;
                        break;
                    case AnimatorControllerParameterType.Int:
                        param.defaultInt = (int)paramData.defaultValue;
                        break;
                    case AnimatorControllerParameterType.Bool:
                        param.defaultBool = (bool)paramData.defaultValue;
                        break;
                }
            }
        }
    }

    private static void RestoreLayers(AnimatorController controller, List<AnimatorLayerData> layers)
    {
        // Remove a camada padrao se existir
        while (controller.layers.Length > 0)
        {
            controller.RemoveLayer(0);
        }

        foreach (var layerData in layers)
        {
            AnimatorControllerLayer newLayer = new AnimatorControllerLayer();
            newLayer.name = layerData.name;
            newLayer.defaultWeight = layerData.weight;

            System.Enum.TryParse<AnimatorLayerBlendingMode>(layerData.blendingMode, out var blendingMode);
            newLayer.blendingMode = blendingMode;

            newLayer.stateMachine = new AnimatorStateMachine();
            newLayer.stateMachine.name = layerData.name;
            newLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;

            RestoreStates(newLayer.stateMachine, layerData.states, controller);

            controller.AddLayer(newLayer);
        }
    }

    private static void RestoreStates(AnimatorStateMachine stateMachine, List<AnimatorStateData> states, AnimatorController controller)
    {
        Dictionary<string, AnimatorState> createdStates = new Dictionary<string, AnimatorState>();
        Dictionary<string, AnimatorStateMachine> createdSubMachines = new Dictionary<string, AnimatorStateMachine>();

        // Primeiro, cria todos os estados
        foreach (var stateData in states)
        {
            if (stateData.type == "State")
            {
                Vector2 position = new Vector2(stateData.position.x, stateData.position.y);
                AnimatorState newState = stateMachine.AddState(stateData.name, position);
                createdStates[stateData.name] = newState;

                newState.speed = stateData.speed;
                newState.mirror = stateData.mirror;
                newState.cycleOffset = stateData.cycleOffset;
                newState.writeDefaultValues = stateData.writeDefaultValues;

                if (!string.IsNullOrEmpty(stateData.motionName))
                {
                    Motion motion = FindMotion(stateData.motionName);
                    if (motion != null)
                    {
                        newState.motion = motion;
                    }
                    else if (stateData.blendTreeData != null)
                    {
                        BlendTree blendTree = CreateBlendTree(stateData.blendTreeData);
                        newState.motion = blendTree;
                    }
                }
            }
            else if (stateData.type == "SubStateMachine")
            {
                Vector2 position = new Vector2(stateData.position.x, stateData.position.y);
                AnimatorStateMachine subMachine = stateMachine.AddStateMachine(stateData.name, position);
                createdSubMachines[stateData.name] = subMachine;

                if (stateData.subStates != null && stateData.subStates.Count > 0)
                {
                    RestoreStates(subMachine, stateData.subStates, controller);
                }
            }
        }

        // Depois, adiciona as transicoes
        foreach (var stateData in states)
        {
            if (stateData.type == "State" && createdStates.ContainsKey(stateData.name))
            {
                AnimatorState currentState = createdStates[stateData.name];

                foreach (var transitionData in stateData.transitions)
                {
                    if (createdStates.ContainsKey(transitionData.destinationState))
                    {
                        AnimatorStateTransition transition = currentState.AddTransition(createdStates[transitionData.destinationState]);
                        ApplyTransitionSettings(transition, transitionData);
                    }
                }
            }
            else if (stateData.type == "AnyState" && stateData.transitions.Count > 0)
            {
                foreach (var transitionData in stateData.transitions)
                {
                    if (createdStates.ContainsKey(transitionData.destinationState))
                    {
                        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(createdStates[transitionData.destinationState]);
                        ApplyTransitionSettings(transition, transitionData);
                    }
                }
            }
        }

        // Configura estado default
        foreach (var stateData in states)
        {
            if (stateData.type == "Entry" && !string.IsNullOrEmpty(stateData.defaultStateName))
            {
                if (createdStates.ContainsKey(stateData.defaultStateName))
                {
                    stateMachine.defaultState = createdStates[stateData.defaultStateName];
                }
            }
        }
    }

    private static void ApplyTransitionSettings(AnimatorStateTransition transition, AnimatorTransitionData data)
    {
        transition.hasExitTime = data.hasExitTime;
        transition.exitTime = data.exitTime;
        transition.duration = data.duration;
        transition.offset = data.offset;
        transition.canTransitionToSelf = data.canTransitionToSelf;

        foreach (var conditionData in data.conditions)
        {
            AnimatorConditionMode mode = AnimatorConditionMode.If;
            System.Enum.TryParse(conditionData.mode, out mode);

            transition.AddCondition(mode, conditionData.threshold, conditionData.parameter);
        }
    }

    private static BlendTree CreateBlendTree(BlendTreeData data)
    {
        BlendTree blendTree = new BlendTree();
        blendTree.name = data.name;
        blendTree.blendType = System.Enum.Parse<BlendTreeType>(data.blendType);
        blendTree.blendParameter = data.blendParameter;
        blendTree.blendParameterY = data.blendParameterY;

        List<ChildMotion> children = new List<ChildMotion>();
        foreach (var childData in data.children)
        {
            ChildMotion child = new ChildMotion();
            child.threshold = childData.threshold;
            child.position = new Vector2(childData.position.x, childData.position.y);
            child.timeScale = childData.timeScale;
            child.cycleOffset = childData.cycleOffset;

            if (childData.blendTreeData != null)
            {
                child.motion = CreateBlendTree(childData.blendTreeData);
            }
            else
            {
                child.motion = FindMotion(childData.motionName);
            }

            children.Add(child);
        }

        blendTree.children = children.ToArray();
        return blendTree;
    }

    private static Motion FindMotion(string motionName)
    {
        if (string.IsNullOrEmpty(motionName) || motionName == "None")
            return null;

        // Procura por Animation Clip
        string[] clipGuids = AssetDatabase.FindAssets($"{motionName} t:AnimationClip");
        if (clipGuids.Length > 0)
        {
            string clipPath = AssetDatabase.GUIDToAssetPath(clipGuids[0]);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        }

        // Procura por Blend Tree
        string[] blendGuids = AssetDatabase.FindAssets($"{motionName} t:BlendTree");
        if (blendGuids.Length > 0)
        {
            string blendPath = AssetDatabase.GUIDToAssetPath(blendGuids[0]);
            return AssetDatabase.LoadAssetAtPath<BlendTree>(blendPath);
        }

        Debug.LogWarning($"Motion nao encontrado: {motionName}");
        return null;
    }

    private static string GetRelativePath(string absolutePath)
    {
        string dataPath = Application.dataPath;
        if (absolutePath.StartsWith(dataPath))
        {
            return "Assets" + absolutePath.Substring(dataPath.Length);
        }
        return absolutePath;
    }

    // ============================================================================
    // JANELA DE CONFIGURACAO
    // ============================================================================

    [MenuItem("Tools/Animator Backup/Configuracoes")]
    public static void ShowWindow()
    {
        AnimatorBackupTool window = GetWindow<AnimatorBackupTool>("Animator Backup Tool");
        window.minSize = new Vector2(400, 300);
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Animator Backup Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Esta ferramenta permite fazer backup completo de Animator Controllers,\n" +
            "incluindo estados, transicoes, blend trees, parametros e layout visual.",
            MessageType.Info);

        GUILayout.Space(10);

        GUILayout.Label("Configuracoes", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Pasta de Backup:", GUILayout.Width(100));
        EditorGUILayout.LabelField(backupRootFolder);
        if (GUILayout.Button("Alterar", GUILayout.Width(60)))
        {
            ConfigureBackupFolder();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.Label("Acoes", EditorStyles.boldLabel);

        if (GUILayout.Button("Backup do Animator Selecionado", GUILayout.Height(30)))
        {
            BackupAnimator();
        }

        if (GUILayout.Button("Batch Backup - Todos os Animators", GUILayout.Height(30)))
        {
            BatchBackupAllAnimators();
        }

        if (GUILayout.Button("Restaurar Animator", GUILayout.Height(30)))
        {
            RestoreAnimator();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Abrir Pasta de Backup", GUILayout.Height(25)))
        {
            OpenBackupFolder();
        }

        EditorGUILayout.EndScrollView();
    }
}

// ============================================================================
// CLASSES DE DADOS PARA SERIALIZACAO
// ============================================================================

[System.Serializable]
public class AnimatorBackupData
{
    public string controllerName;
    public string controllerPath;
    public string backupDate;
    public string unityVersion;
    public List<AnimatorParameterData> parameters;
    public List<AnimatorLayerData> layers;
}

[System.Serializable]
public class AnimatorParameterData
{
    public string name;
    public string type;
    public object defaultValue;
}

[System.Serializable]
public class AnimatorLayerData
{
    public string name;
    public float weight;
    public string blendingMode;
    public List<AnimatorStateData> states;
}

[System.Serializable]
public class AnimatorStateData
{
    public string name;
    public string type;
    public Vector2Data position;
    public string motionName;
    public string motionType;
    public float speed;
    public bool mirror;
    public float cycleOffset;
    public bool writeDefaultValues;
    public string defaultStateName;
    public BlendTreeData blendTreeData;
    public List<AnimatorStateData> subStates;
    public List<AnimatorTransitionData> transitions;
}

[System.Serializable]
public class AnimatorTransitionData
{
    public string destinationState;
    public bool hasExitTime;
    public float exitTime;
    public float duration;
    public float offset;
    public float transitionDuration;
    public bool canTransitionToSelf;
    public List<AnimatorConditionData> conditions;
}

[System.Serializable]
public class AnimatorConditionData
{
    public string parameter;
    public string mode;
    public float threshold;
}

[System.Serializable]
public class BlendTreeData
{
    public string name;
    public string blendType;
    public string blendParameter;
    public string blendParameterY;
    public List<BlendTreeChildData> children;
}

[System.Serializable]
public class BlendTreeChildData
{
    public string motionName;
    public float threshold;
    public Vector2Data position;
    public float timeScale;
    public float cycleOffset;
    public BlendTreeData blendTreeData;
}

[System.Serializable]
public class Vector2Data
{
    public float x;
    public float y;

    public Vector2Data(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public Vector2 ToVector2()
    {
        return new Vector2(x, y);
    }
}