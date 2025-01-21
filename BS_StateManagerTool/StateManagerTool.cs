using System.Collections.Generic;
using System.IO;
using System.Linq;
using StateSystem;
using UnityEditor;
using UnityEngine;

public class StateManagerTool : EditorWindow
{
    private enum ToolMode { CreateStateManager, AddState }
    private ToolMode currentMode = ToolMode.CreateStateManager;

    private string stateManagerName = "NewStateManager";
    private MonoScript selectedStateManagerScript;

    private string newStateName = "NewState";
    private string[] stateNames = new string[] { "State1", "State2" };

    private bool generateTransitions = true;
    private Object destinationFolder;
    private string customDestinationPath = "Assets";

    private string templatePath;
    private static string scriptDirectory;

    [MenuItem("States Tool/State Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<StateManagerTool>("State Manager Tool");
        Texture icon = AssetDatabase.LoadAssetAtPath<Texture>(Path.Combine(scriptDirectory, "Icons/StateTool.png"));
        window.titleContent = new GUIContent("State Manager", icon);
        window.minSize = new Vector2(500, 400);
    }

    private void OnEnable()
    {
        string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
        scriptDirectory = Path.GetDirectoryName(scriptPath);
        templatePath = Path.Combine(scriptDirectory, "Templates");

        if (!Directory.Exists(templatePath))
            Debug.LogError($"Templates folder not found at {templatePath}. Please create a Templates folder and add your templates.");
    }

    private void OnGUI()
    {
        GUILayout.Label("State Manager Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        currentMode = (ToolMode)EditorGUILayout.EnumPopup("Mode:", currentMode);
        GUILayout.Space(20);

        if (currentMode == ToolMode.CreateStateManager)
            DrawCreateStateManagerUI();
        else if (currentMode == ToolMode.AddState)
            DrawAddStateUI();
    }

    private void DrawCreateStateManagerUI()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("Create a New StateManager", EditorStyles.boldLabel);

        // StateManager Name
        GUILayout.BeginHorizontal();
        GUILayout.Label("StateManager Name:", GUILayout.Width(150));
        stateManagerName = EditorGUILayout.TextField(stateManagerName);
        GUILayout.EndHorizontal();

        // States Configuration
        GUILayout.Label("States Configuration", EditorStyles.boldLabel);

        GUILayout.BeginVertical("box");
        for (int i = 0; i < stateNames.Length; i++)
        {
            GUILayout.BeginHorizontal();
            stateNames[i] = EditorGUILayout.TextField($"State {i + 1}:", stateNames[i]);
            if (GUILayout.Button("X", GUILayout.Width(20)))
                RemoveState(i);
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        if (GUILayout.Button("Add State", GUILayout.Width(100)))
            AddState();

        // Generate Transitions Toggle
        generateTransitions = EditorGUILayout.Toggle("Generate Transitions in StateManager", generateTransitions);

        // Destination Folder
        GUILayout.Label("Destination Folder", EditorStyles.boldLabel);
        destinationFolder = EditorGUILayout.ObjectField("Folder", destinationFolder, typeof(DefaultAsset), false);

        if (destinationFolder != null)
        {
            string folderPath = AssetDatabase.GetAssetPath(destinationFolder);
            if (AssetDatabase.IsValidFolder(folderPath))
                customDestinationPath = folderPath;
            else
                EditorGUILayout.HelpBox("Selected object is not a valid folder. Please select a folder.", MessageType.Error);
        }

        GUILayout.Label($"Current Path: {customDestinationPath}", EditorStyles.helpBox);

        GUILayout.Space(10);

        // Generate Button
        GUI.enabled = !string.IsNullOrEmpty(stateManagerName) && destinationFolder != null;
        if (GUILayout.Button("Generate StateManager and States", GUILayout.Height(40)))
            GenerateStateManagerAndStates();
        GUI.enabled = true;

        GUILayout.EndVertical();
    }

    private void DrawAddStateUI()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("Add a State to an Existing StateManager", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("Drag and drop a StateManager script here, then add a new State.", MessageType.Info);

        // Drag & Drop StateManager
        selectedStateManagerScript = (MonoScript)EditorGUILayout.ObjectField(
            "StateManager Script",
            selectedStateManagerScript,
            typeof(MonoScript),
            false
        );

        if (selectedStateManagerScript != null)
        {
            var scriptType = selectedStateManagerScript.GetClass();
            if (scriptType == null || !typeof(StateManager).IsAssignableFrom(scriptType))
            {
                EditorGUILayout.HelpBox("Selected script does not inherit from StateManager.", MessageType.Error);
                selectedStateManagerScript = null;
            }
        }

        GUILayout.Space(5);

        // State Name
        GUILayout.BeginHorizontal();
        GUILayout.Label("State Name:", GUILayout.Width(150));
        newStateName = EditorGUILayout.TextField(newStateName);
        GUILayout.EndHorizontal();

        // Generate Transitions Toggle
        generateTransitions = EditorGUILayout.Toggle("Generate Transition in StateManager", generateTransitions);

        GUILayout.Space(10);

        // Add State Button
        GUI.enabled = selectedStateManagerScript != null && !string.IsNullOrEmpty(newStateName);
        if (GUILayout.Button("Add State", GUILayout.Height(40)))
            AddStateToStateManager();
        GUI.enabled = true;

        GUILayout.EndVertical();
    }

    private void AddState()
    {
        ArrayUtility.Add(ref stateNames, $"State{stateNames.Length + 1}");
    }

    private void RemoveState(int index)
    {
        ArrayUtility.RemoveAt(ref stateNames, index);
    }

    private void GenerateStateManagerAndStates()
    {
        if (!ValidateInputsForStateManager()) return;

        string stateManagerFolder = Path.Combine(customDestinationPath, stateManagerName);
        if (!AssetDatabase.IsValidFolder(stateManagerFolder))
            AssetDatabase.CreateFolder(customDestinationPath, stateManagerName);

        GenerateStateManagerScript(stateManagerFolder);
        foreach (string stateName in stateNames)
            GenerateStateScript(stateManagerFolder, stateName);

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", $"StateManager and States generated in {stateManagerFolder}", "OK");
    }

    private bool DoesScriptExist(string scriptName)
    {
        string[] guids = AssetDatabase.FindAssets($"{scriptName} t:Script");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);

            if (fileName == scriptName)
                return true;
        }
        return false;
    }

    private void AddStateToStateManager()
    {
        string selectedStateManagerPath = AssetDatabase.GetAssetPath(selectedStateManagerScript);
        string selectedStateManagerFolder = Path.GetDirectoryName(selectedStateManagerPath);

        if (DoesScriptExist(newStateName))
        {
            EditorUtility.DisplayDialog(
                "State Exists",
                $"A script named '{newStateName}.cs' already exists in the project. No transition will be added.",
                "OK"
            );
            
            Debug.LogWarning($"State '{newStateName}' already exists. No transition added to {selectedStateManagerScript.name}.");
            return; 
        }

        GenerateStateScript(selectedStateManagerFolder, newStateName);

        if (generateTransitions)
            AddTransitionToStateManager(selectedStateManagerFolder, newStateName, selectedStateManagerScript.name);

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", $"State {newStateName} added to {selectedStateManagerScript.name}.", "OK");
    }




    private bool ValidateInputsForStateManager()
    {
        if (string.IsNullOrEmpty(stateManagerName))
        {
            EditorUtility.DisplayDialog("Error", "State Manager Name cannot be empty.", "OK");
            return false;
        }

        if (destinationFolder == null || !AssetDatabase.IsValidFolder(customDestinationPath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a valid destination folder.", "OK");
            return false;
        }

        return true;
    }

    private void GenerateStateManagerScript(string folderPath)
    {
        string template = LoadTemplate("StateManagerTemplate.txt");
        if (string.IsNullOrEmpty(template))
        {
            Debug.LogError("StateManager template not found!");
            return;
        }

        string transitionsCode = string.Empty;
        string initialStateCode = string.Empty;

        foreach (var stateName in stateNames)
        {
            if (!DoesScriptExist(stateName))
            {
                transitionsCode += $@"
    public void TransitionTo{stateName}()
    {{
        SetState(new {stateName}(this));
    }}
";
            }
            else
                Debug.LogWarning($"Skipping transition for existing state: {stateName}");
            
        }

        if (!DoesScriptExist(stateNames[0]))
        {
            initialStateCode = $@"
    private void Start()
    {{
        // Example: Set initial state
        SetState(new {stateNames[0]}(this));
    }}
";
        }
        else
            Debug.LogWarning($"Skipping initial state setup for existing state: {stateNames[0]}");
        
        string content = template
            .Replace("{{StateManagerName}}", stateManagerName)
            .Replace("{{FirstStateName}}", stateNames.Length > 0 ? stateNames[0] : "InitialState")
            .Replace("{{Transitions}}", transitionsCode)
            .Replace("{{InitialStateSetup}}", initialStateCode);

        string filePath = Path.Combine(folderPath, $"{stateManagerName}.cs");

        File.WriteAllText(filePath, content);

        Debug.Log($"StateManager script generated at {filePath}");
    }



    private void GenerateStateScript(string folderPath, string stateName)
    {
        string template = LoadTemplate("StateTemplate.txt");
        if (string.IsNullOrEmpty(template))
        {
            Debug.LogError("State template not found!");
            return;
        }

        if (DoesScriptExist(stateName))
        {
            Debug.LogWarning($"State script '{stateName}.cs' already exists in the project. Skipping creation.");
            return;
        }

        string filePath = Path.Combine(folderPath, $"{stateName}.cs");

        string stateManagerName = selectedStateManagerScript != null
            ? selectedStateManagerScript.name
            : this.stateManagerName;

        string content = template
            .Replace("{{StateName}}", stateName)
            .Replace("{{StateManagerName}}", stateManagerName);

        File.WriteAllText(filePath, content);

        Debug.Log($"State script generated: {filePath}");
    }


    private string LoadTemplate(string fileName)
    {
        string path = Path.Combine(templatePath, fileName);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    private void AddTransitionToStateManager(string folderPath, string stateName, string stateManagerName)
    {
        string stateManagerScriptPath = Path.Combine(folderPath, $"{stateManagerName}.cs");

        if (!File.Exists(stateManagerScriptPath))
        {
            Debug.LogError($"StateManager script not found: {stateManagerScriptPath}");
            return;
        }

        string content = File.ReadAllText(stateManagerScriptPath);

        string transitionMethod = $@"
    public void TransitionTo{stateName}()
    {{
        SetState(new {stateName}(this));
    }}
";
        if (content.Contains($"TransitionTo{stateName}"))
        {
            Debug.LogWarning($"TransitionTo{stateName} already exists in {stateManagerScriptPath}. Skipping addition.");
            return;
        }

        int insertionIndex = content.LastIndexOf('}');
        content = content.Insert(insertionIndex - 1, transitionMethod);

        File.WriteAllText(stateManagerScriptPath, content);

        Debug.Log($"TransitionTo{stateName} added to {stateManagerName}");
    }

}
