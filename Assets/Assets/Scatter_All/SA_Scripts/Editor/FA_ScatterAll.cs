#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace Bukyja.ScatterAll
{
    public class FA_ScatterAll : EditorWindow
    {
        [Serializable]
        public class AllCategories
        {
            public List<CategoryPreset> categories = new List<CategoryPreset>();
        }

        [Serializable]
        public class CategoryPreset
        {
            public string categoryName;
            public List<MeshData> meshData = new List<MeshData>();
            [NonSerialized] public int selectedMeshIndex = -1;
        }

        [Serializable]
        public class MeshData
        {
            public string meshPath = "";
            [NonSerialized] public GameObject mesh;
            public float rotationMin = 0f;
            public float rotationMax = 360f;
            public float scaleMin = 0.1f;
            public float scaleMax = 1f;
            public float offsetY = 0f;
            public bool castShadows = true;
            // Nuova proprietà per ricevere ombre
            public bool receiveShadows = true;
            public float tiltFactor = 0f;

            [Flags]
            public enum StaticFlags
            {
                None = 0,
                BatchingStatic = 1 << 0,
                OccluderStatic = 1 << 1,
                OccludeeStatic = 1 << 2,
                ReflectionProbeStatic = 1 << 3,
                LightmapStatic = 1 << 4,
                ContributeGIStatic = 1 << 6,
            }

            public StaticFlags staticFlags = StaticFlags.None;
        }

        [MenuItem("Tools/Bukyja/Scatter-All")]
        public static void ShowWindow()
        {
            var window = GetWindow<FA_ScatterAll>("Scatter-All");
            window.minSize = new Vector2(400, 400);
            window.Show();
        }

        private AllCategories allCats;
        private string presetFilePath = "";
        private int selectedCategoryIndex = -1;
        private int previousCategoryIndex = -1;

        private float spawnDistance = 1f;
        private Vector3 lastSpawnPos = Vector3.positiveInfinity;

        private Vector2 scrollPos;
        private const float cellWidth = 200f;

        private const string EditorPrefsPresetPathKey = "MeshPainterPresetPath";

        private bool isSpawning = false;
        private float spawnCircleRadius = 5f;
        private float yRange = 5f;
        private bool isDirty = false;
        private Texture2D logo;

        private bool wasSceneViewFocused;

        private void OnDestroy()
        {
            if (isDirty && allCats != null)
            {
                bool shouldSave = EditorUtility.DisplayDialog(
                    "Save Changes?",
                    "There are unsaved changes. Do you want to save the preset?",
                    "Save",
                    "Cancel"
                );
                if (shouldSave) SavePreset();
            }
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += CheckSceneViewFocus;

            presetFilePath = EditorPrefs.GetString(EditorPrefsPresetPathKey, "");
            if (!string.IsNullOrEmpty(presetFilePath))
            {
                LoadPresetFromPath(presetFilePath);
            }

            logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Scatter_All/SA_Scripts/Editor/Logo.png");
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.update -= CheckSceneViewFocus;
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();

            if (logo != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(logo, GUILayout.Width(200), GUILayout.Height(200));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }

            if (string.IsNullOrEmpty(presetFilePath))
            {
                GUILayout.Label("No preset loaded.");
            }
            else
            {
                string presetName = Path.GetFileNameWithoutExtension(presetFilePath);
                GUILayout.Label("Current preset: " + presetName, EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Preset")) LoadPreset();
            if (GUILayout.Button("Save Preset")) SavePreset();
            if (GUILayout.Button("Unload Preset")) UnloadPreset();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (allCats == null)
            {
                if (GUILayout.Button("Create New Preset"))
                {
                    CreateBlankPreset();
                }
                if (EditorGUI.EndChangeCheck()) isDirty = true;
                return;
            }

            if (allCats.categories.Count == 0)
            {
                GUILayout.Label("No categories found. Add one below.");
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Category"))
{
    // Mostra la finestra per inserire il nome della categoria
    CategoryNameWindow.Show(categoryName =>
    {
        if (string.IsNullOrEmpty(categoryName))
        {
            Debug.Log("Category creation canceled.");
            return;
        }

        // Trova o crea il parent ScatterAll_Categories
        GameObject categoriesParent = GameObject.Find("ScatterAll_Categories");
        if (categoriesParent == null)
        {
            categoriesParent = new GameObject("ScatterAll_Categories");
            Undo.RegisterCreatedObjectUndo(categoriesParent, "Create ScatterAll_Categories");
            Debug.Log("ScatterAll_Categories parent created.");
        }

        // Crea un nuovo GameObject per la categoria
        GameObject newCategoryObject = new GameObject(categoryName);
        Undo.RegisterCreatedObjectUndo(newCategoryObject, "Create Category");
        newCategoryObject.transform.parent = categoriesParent.transform;

        Debug.Log($"New category created: {categoryName}");

        // Aggiungi la categoria alla lista
        var newCat = new CategoryPreset { categoryName = categoryName };
        allCats.categories.Add(newCat);
        selectedCategoryIndex = allCats.categories.Count - 1;

        Debug.Log($"Category '{categoryName}' added to the internal list.");
    });
}

            if (allCats.categories.Count > 0 && GUILayout.Button("Remove Category"))
            {
                if (selectedCategoryIndex >= 0 && selectedCategoryIndex < allCats.categories.Count)
                {
                    allCats.categories.RemoveAt(selectedCategoryIndex);
                    selectedCategoryIndex = Mathf.Clamp(selectedCategoryIndex, 0, allCats.categories.Count - 1);
                    previousCategoryIndex = selectedCategoryIndex;
                    EditorPrefs.SetString(EditorPrefsPresetPathKey, presetFilePath);
                }
            }

            if (GUILayout.Button("Add Mask"))
            {
                // Trova il GameObject "ScatterAll_Masks"
                GameObject masksParent = GameObject.Find("ScatterAll_Masks");

                // Se non esiste, crealo
                if (masksParent == null)
                {
                    masksParent = new GameObject("ScatterAll_Masks");
                    Undo.RegisterCreatedObjectUndo(masksParent, "Create ScatterAll_Masks");
                    Debug.Log("ScatterAll_Masks parent created.");
                }

                // Genera un nuovo GameObject figlio con nome incrementale
                int childCount = masksParent.transform.childCount;
                string newMaskName = $"Mask_{childCount + 1}";
                GameObject newMask = new GameObject(newMaskName);
                Undo.RegisterCreatedObjectUndo(newMask, "Create Mask");

                // Imposta il parent del nuovo GameObject
                newMask.transform.parent = masksParent.transform;

                // Aggiungi il componente ScatterMask al nuovo GameObject
                ScatterMask scatterMask = newMask.AddComponent<ScatterMask>();
                if (scatterMask != null)
                {
                    Debug.Log($"ScatterMask component added to {newMaskName}");
                }
                else
                {
                    Debug.LogError($"Failed to add ScatterMask to {newMaskName}");
                }
            }

             


            EditorGUILayout.EndHorizontal();

            if (allCats.categories.Count > 0)
            {
                string[] catNames = new string[allCats.categories.Count];
                for (int i = 0; i < catNames.Length; i++)
                    catNames[i] = allCats.categories[i].categoryName;

                int oldCategoryIndex = selectedCategoryIndex;
                selectedCategoryIndex = EditorGUILayout.Popup("Category", selectedCategoryIndex, catNames);

                if (selectedCategoryIndex != oldCategoryIndex)
                {
                    if (selectedCategoryIndex >= 0 && selectedCategoryIndex < allCats.categories.Count)
                        allCats.categories[selectedCategoryIndex].selectedMeshIndex = -1;
                    previousCategoryIndex = selectedCategoryIndex;
                    EditorPrefs.SetString(EditorPrefsPresetPathKey, presetFilePath);
                }

                if (selectedCategoryIndex < 0) selectedCategoryIndex = 0;
                if (selectedCategoryIndex >= allCats.categories.Count) selectedCategoryIndex = allCats.categories.Count - 1;

                spawnDistance = EditorGUILayout.FloatField(
                    new GUIContent("Spawn Distance Offset", "Minimum distance between consecutive spawns."),
                    spawnDistance
                );
                if (spawnDistance < 0.01f)
                {
                    spawnDistance = 0.01f;
                    EditorGUILayout.HelpBox("Spawn Distance Offset cannot be less than 0.01.", MessageType.Warning);
                }

                spawnCircleRadius = EditorGUILayout.FloatField(
                    new GUIContent("Spawn Circle Radius", "The radius of the spawn circle used to delete meshes."),
                    spawnCircleRadius
                );
                if (spawnCircleRadius <= 0f)
                {
                    spawnCircleRadius = 0.5f;
                    EditorGUILayout.HelpBox("Spawn Circle Radius must be greater than 0.", MessageType.Warning);
                }

                yRange = Mathf.Clamp(yRange, 0f, 10f);

                DrawCategoryUI(allCats.categories[selectedCategoryIndex]);
            }

            if (EditorGUI.EndChangeCheck()) isDirty = true;
        }

        private void DrawCategoryUI(CategoryPreset cat)
        {
            cat.categoryName = EditorGUILayout.TextField("Category Name", cat.categoryName);
            EditorGUILayout.Space();

            if (GUILayout.Button("Add Mesh"))
            {
                cat.meshData.Add(new MeshData());
            }

            float availableWidth = EditorGUIUtility.currentViewWidth - 40f;
            int columns = Mathf.FloorToInt(availableWidth / cellWidth);
            if (columns < 1) columns = 1;

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            for (int i = 0; i < cat.meshData.Count; i++)
            {
                bool removed = false;

                if (i % columns == 0)
                    EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginVertical("box", GUILayout.Width(cellWidth));
                var data = cat.meshData[i];

                if (data.mesh == null && !string.IsNullOrEmpty(data.meshPath))
                {
                    data.mesh = AssetDatabase.LoadAssetAtPath<GameObject>(data.meshPath);
                    if (data.mesh == null) data.meshPath = "";
                }

                Texture2D preview = null;
                if (data.mesh != null)
                {
                    preview = AssetPreview.GetAssetPreview(data.mesh);
                    if (preview == null) preview = AssetPreview.GetMiniThumbnail(data.mesh);
                }

                GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
                if (i == cat.selectedMeshIndex)
                {
                    int borderThickness = 4;
                    btnStyle.border = new RectOffset(borderThickness, borderThickness, borderThickness, borderThickness);
                    btnStyle.overflow = new RectOffset(0, 0, 0, 0);
                    btnStyle.padding = new RectOffset(0, 0, 0, 0);
                    btnStyle.normal.background = MakeBorderTex(128, 128, new Color(0.2f, 1f, 0.2f, 1f), borderThickness);
                }

                if (preview != null)
                {
                    if (GUILayout.Button(preview, btnStyle, GUILayout.Width(100), GUILayout.Height(100)))
                    {
                        if (cat.selectedMeshIndex == i) cat.selectedMeshIndex = -1;
                        else cat.selectedMeshIndex = i;
                    }
                    GUILayout.Label(data.mesh.name, EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    if (GUILayout.Button("No mesh", btnStyle, GUILayout.Width(100), GUILayout.Height(100)))
                    {
                        if (cat.selectedMeshIndex == i) cat.selectedMeshIndex = -1;
                        else cat.selectedMeshIndex = i;
                    }
                }

                EditorGUILayout.Space();

                data.rotationMin = EditorGUILayout.Slider("Rot Min", data.rotationMin, 0f, 360f);
                data.rotationMax = EditorGUILayout.Slider("Rot Max", data.rotationMax, 0f, 360f);

                data.scaleMin = EditorGUILayout.Slider("Sca Min", data.scaleMin, 0.1f, 3f);
                data.scaleMax = EditorGUILayout.Slider("Sca Max", data.scaleMax, 0.1f, 3f);
                if (data.scaleMax < data.scaleMin) data.scaleMax = data.scaleMin;

                data.offsetY = EditorGUILayout.FloatField("Y Offset", data.offsetY);
                data.offsetY = Mathf.Clamp(data.offsetY, -5f, 5f);
                if (data.offsetY < -5f || data.offsetY > 5f)
                {
                    EditorGUILayout.HelpBox("Y Offset must be within -5 to 5.", MessageType.Warning);
                }

                data.castShadows = EditorGUILayout.Toggle("Cast Shadows", data.castShadows);

                // Verifica se si sta usando la Built-in Render Pipeline
                if (IsUsingBuiltInPipeline())
                {
                    data.receiveShadows = EditorGUILayout.Toggle("Receive Shadows", data.receiveShadows);
                }

                EditorGUILayout.Space();
                data.tiltFactor = EditorGUILayout.Slider("Tilt Factor (0..1)", data.tiltFactor, 0f, 1f);
                float tiltDegrees = 45f * data.tiltFactor;
                EditorGUILayout.LabelField($"Tilt Range: -{tiltDegrees:F1}° / +{tiltDegrees:F1}°");

                EditorGUILayout.Space();
                data.staticFlags = (MeshData.StaticFlags)EditorGUILayout.EnumFlagsField("Static Flags", data.staticFlags);

                EditorGUILayout.Space();
                var oldMesh = data.mesh;
                data.mesh = (GameObject)EditorGUILayout.ObjectField(data.mesh, typeof(GameObject), false, GUILayout.Width(180));
                if (data.mesh != oldMesh)
                {
                    if (data.mesh != null) data.meshPath = AssetDatabase.GetAssetPath(data.mesh);
                    else data.meshPath = "";
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    cat.meshData.RemoveAt(i);
                    if (cat.selectedMeshIndex == i) cat.selectedMeshIndex = -1;
                    removed = true;
                }

                EditorGUILayout.EndVertical();

                if (removed)
                {
                    if (i % columns == columns - 1 || i == cat.meshData.Count - 1)
                        EditorGUILayout.EndHorizontal();
                    break;
                }

                if (i % columns == columns - 1 || i == cat.meshData.Count - 1)
                    EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        // Metodo per verificare se si sta usando la Built-in Render Pipeline
        private bool IsUsingBuiltInPipeline()
        {
            // Se renderPipelineAsset è null, si sta usando la Built-in Render Pipeline
            return GraphicsSettings.defaultRenderPipeline == null;
        }

        private void CreateBlankPreset()
        {
            allCats = new AllCategories();
            presetFilePath = "";
            selectedCategoryIndex = -1;
            previousCategoryIndex = -1;
            EditorPrefs.DeleteKey(EditorPrefsPresetPathKey);
            isDirty = true;
        }

        private void SavePreset()
        {
            if (allCats == null) return;
            if (string.IsNullOrEmpty(presetFilePath))
            {
                string path = EditorUtility.SaveFilePanel("Save Preset (JSON)", "Assets", "NewPreset", "json");
                if (string.IsNullOrEmpty(path)) return;
                presetFilePath = path;
            }

            string json = JsonUtility.ToJson(allCats, true);
            File.WriteAllText(presetFilePath, json);
            EditorPrefs.SetString(EditorPrefsPresetPathKey, presetFilePath);
            isDirty = false;
        }

        private void LoadPreset()
        {
            string path = EditorUtility.OpenFilePanel("Load Preset (JSON)", "Assets", "json");
            if (string.IsNullOrEmpty(path)) return;
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            var loaded = JsonUtility.FromJson<AllCategories>(json);
            if (loaded == null) return;

            allCats = loaded;
            presetFilePath = path;

            for (int c = 0; c < allCats.categories.Count; c++)
            {
                var cat = allCats.categories[c];
                cat.selectedMeshIndex = -1;
                for (int m = 0; m < cat.meshData.Count; m++)
                {
                    var data = cat.meshData[m];
                    if (!string.IsNullOrEmpty(data.meshPath))
                    {
                        data.mesh = AssetDatabase.LoadAssetAtPath<GameObject>(data.meshPath);
                    }
                }
            }

            if (allCats.categories.Count > 0)
            {
                selectedCategoryIndex = 0;
                previousCategoryIndex = 0;
            }
            else
            {
                selectedCategoryIndex = -1;
                previousCategoryIndex = -1;
            }

            EditorPrefs.SetString(EditorPrefsPresetPathKey, presetFilePath);
            isDirty = false;
        }

        private void LoadPresetFromPath(string path)
        {
            if (!File.Exists(path)) return;
            string json = File.ReadAllText(path);
            var loaded = JsonUtility.FromJson<AllCategories>(json);
            if (loaded == null) return;

            allCats = loaded;
            presetFilePath = path;

            for (int c = 0; c < allCats.categories.Count; c++)
            {
                var cat = allCats.categories[c];
                cat.selectedMeshIndex = -1;
                for (int m = 0; m < cat.meshData.Count; m++)
                {
                    var data = cat.meshData[m];
                    if (!string.IsNullOrEmpty(data.meshPath))
                    {
                        data.mesh = AssetDatabase.LoadAssetAtPath<GameObject>(data.meshPath);
                    }
                }
            }

            if (allCats.categories.Count > 0)
            {
                selectedCategoryIndex = 0;
                previousCategoryIndex = 0;
            }
            else
            {
                selectedCategoryIndex = -1;
                previousCategoryIndex = -1;
            }

            isDirty = false;
        }

        private void UnloadPreset()
        {
            if (allCats == null) return;
            if (EditorUtility.DisplayDialog(
                "Unload Preset",
                "Are you sure you want to unload the current preset? This will only remove the data from the Editor window without deleting the JSON file.",
                "Yes",
                "No"
            ))
            {
                allCats = null;
                selectedCategoryIndex = -1;
                previousCategoryIndex = -1;
                EditorPrefs.DeleteKey(EditorPrefsPresetPathKey);
                presetFilePath = "";
                Repaint();
                isDirty = false;
            }
        }

        private void CheckSceneViewFocus()
        {
            if (allCats == null || selectedCategoryIndex < 0 || selectedCategoryIndex >= allCats.categories.Count)
                return;

            SceneView sceneView = SceneView.lastActiveSceneView;
            bool isSceneViewFocused = (EditorWindow.focusedWindow == sceneView);

            // Deseleziona la mesh se perdiamo il focus sulla SceneView
            if (!isSceneViewFocused && wasSceneViewFocused)
            {
                var category = allCats.categories[selectedCategoryIndex];
                if (category.selectedMeshIndex >= 0)
                {
                    DeselectMesh();
                }
            }

            wasSceneViewFocused = isSceneViewFocused;
        }


        private void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (!SceneView.currentDrawingSceneView.camera.pixelRect.Contains(e.mousePosition))
                {
                    DeselectMesh();
                    return;
                }
            }

            if (e.control && e.shift)
            {
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                EditorGUIUtility.editingTextField = false;
            }
            if (e.alt) return;
            if (e.button != 0) return;

            bool isAnyMeshSelected = (allCats != null
                                      && selectedCategoryIndex >= 0
                                      && selectedCategoryIndex < allCats.categories.Count
                                      && allCats.categories[selectedCategoryIndex].selectedMeshIndex >= 0);

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.control)
            {
                e.Use();
                if (isAnyMeshSelected)
                {
                    var cat = allCats.categories[selectedCategoryIndex];
                    var idx = cat.selectedMeshIndex;
                    var data = cat.meshData[idx];

                    TryDeleteWithinSpawnVolume(e, data, overrideOffsetY: 0f);
                }
                return;
            }

            if (!isAnyMeshSelected) return;

            var selectedCat = allCats.categories[selectedCategoryIndex];
            if (selectedCat.meshData.Count == 0) return;
            var selectedIdx = selectedCat.selectedMeshIndex;
            if (selectedIdx < 0 || selectedIdx >= selectedCat.meshData.Count) return;
            var selectedData = selectedCat.meshData[selectedIdx];
            if (selectedData.mesh == null) return;

            if (e.control)
            {
                Color circleColor = Color.red;
                DrawRaycastCircle(selectedData, circleColor, overrideOffsetY: 0f);
            }
            else
            {
                if (Mathf.Approximately(selectedData.offsetY, 0f))
                {
                    Color circleColor = Color.green;
                    DrawRaycastCircle(selectedData, circleColor, overrideOffsetY: null);
                }
                else
                {
                    Color cylinderColor = new Color(0f, 1f, 0f, 0.3f);
                    DrawRaycastCylinder(selectedData, cylinderColor, overrideOffsetY: null);
                }
            }

            if (!e.control && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
            {
                bool didSpawn = HandleSpawn(e, selectedData, selectedData.tiltFactor);
                if (didSpawn) e.Use();
            }

            if (e.control)
            {
                var allIDs = UnityEngine.Object.FindObjectsOfType<MeshPainterID>();
                Handles.color = Color.red;
                foreach (var idComp in allIDs)
                {
                    Handles.SphereHandleCap(0, idComp.transform.position, Quaternion.identity, 0.1f, EventType.Repaint);
                }
            }
        }

        private void DeselectMesh()
        {
            if (allCats != null && selectedCategoryIndex >= 0 && selectedCategoryIndex < allCats.categories.Count)
            {
                var category = allCats.categories[selectedCategoryIndex];
                if (category.selectedMeshIndex >= 0)
                {
                    category.selectedMeshIndex = -1;
                    Repaint();
                }
            }
        }

        private void TryDeleteWithinSpawnVolume(Event e, MeshData data, float? overrideOffsetY = null)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            Vector3 center = new Vector3(hit.point.x, 0, hit.point.z);
            var allIDs = UnityEngine.Object.FindObjectsOfType<MeshPainterID>();

            foreach (var idComp in allIDs)
            {
                Vector3 objPos = idComp.transform.position;
                float horizontalDistance = Vector3.Distance(new Vector3(objPos.x, center.y, objPos.z), center);

                float currentOffsetY = overrideOffsetY ?? data.offsetY;

                if (Mathf.Approximately(currentOffsetY, 0f))
                {
                    if (horizontalDistance <= spawnCircleRadius)
                    {
                        Undo.DestroyObjectImmediate(idComp.gameObject);
                        isDirty = true;
                    }
                }
                else
                {
                    if (horizontalDistance <= spawnCircleRadius)
                    {
                        if (currentOffsetY > 0f)
                        {
                            if (objPos.y >= 0f && objPos.y <= currentOffsetY)
                            {
                                Undo.DestroyObjectImmediate(idComp.gameObject);
                                isDirty = true;
                            }
                        }
                        else
                        {
                            if (objPos.y <= 0f && objPos.y >= currentOffsetY)
                            {
                                Undo.DestroyObjectImmediate(idComp.gameObject);
                                isDirty = true;
                            }
                        }
                    }
                }
            }
        }

        private bool HandleSpawn(Event e, MeshData data, float tiltFactor)
        {
            if (isSpawning) return false;
            isSpawning = true;
            try
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (!Physics.Raycast(ray, out RaycastHit hit)) return false;
                if (Vector3.Distance(hit.point, lastSpawnPos) < spawnDistance) return false;

                GameObject parentObject = null;

                // Trova il parent basato sulla categoria selezionata
                if (selectedCategoryIndex >= 0 && selectedCategoryIndex < allCats.categories.Count)
                {
                    string categoryName = allCats.categories[selectedCategoryIndex].categoryName;
                    parentObject = GameObject.Find(categoryName);

                    // Crea il parent se non esiste
                    if (parentObject == null)
                    {
                        parentObject = new GameObject(categoryName);
                        Undo.RegisterCreatedObjectUndo(parentObject, "Create Category Parent");
                    }
                }

                // Instanzia la mesh come figlio dell'oggetto genitore
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(data.mesh, parentObject?.transform);
                if (instance == null)
                {
                    instance = Instantiate(data.mesh, parentObject?.transform);
                }

                var idComp = instance.GetComponent<MeshPainterID>();
                if (idComp == null) idComp = instance.AddComponent<MeshPainterID>();
                idComp.uniqueID = UnityEngine.Random.Range(1, 999999999);

                Vector3 newPosition = instance.transform.position;
                newPosition.x = hit.point.x;
                newPosition.z = hit.point.z;
                newPosition.y = hit.point.y + data.offsetY;
                instance.transform.position = newPosition;

                float randomY = UnityEngine.Random.Range(data.rotationMin, data.rotationMax);
                instance.transform.Rotate(Vector3.up, randomY, Space.World);

                float randomTiltX = UnityEngine.Random.Range(-45f * tiltFactor, 45f * tiltFactor);
                instance.transform.Rotate(Vector3.right, randomTiltX, Space.Self);

                float rndScale = UnityEngine.Random.Range(data.scaleMin, data.scaleMax);
                if (rndScale < 0.1f) rndScale = 0.1f;
                instance.transform.localScale = Vector3.one * rndScale;

                ConfigureShadows(instance, data);
                ConfigureStaticFlags(instance, data.staticFlags); // Assicurati di passare staticFlags

                Undo.RegisterCreatedObjectUndo(instance, "Spawn Mesh");
                lastSpawnPos = hit.point;
                isDirty = true;

                return true;
            }
            finally
            {
                isSpawning = false;
            }
        }


        private void DrawRaycastCircle(MeshData data, Color color, float? overrideOffsetY = null)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Handles.color = color;
                Handles.DrawWireDisc(hit.point, Vector3.up, spawnCircleRadius);

                if (!Mathf.Approximately(data.offsetY, 0f) && color != Color.red)
                {
                    Vector3 arrowDirection = data.offsetY > 0 ? Vector3.up : Vector3.down;
                    Vector3 arrowPosition = hit.point + arrowDirection * Mathf.Abs(data.offsetY) * 0.5f;
                    float arrowSize = Mathf.Abs(data.offsetY);

                    Handles.color = Color.yellow;
                    Handles.ArrowHandleCap(0, arrowPosition, Quaternion.LookRotation(arrowDirection), arrowSize, EventType.Repaint);
                }
            }
        }

        private void DrawRaycastCylinder(MeshData data, Color color, float? overrideOffsetY = null)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Handles.color = color;

                Vector3 baseCenter = hit.point;
                float currentOffsetY = overrideOffsetY ?? data.offsetY;
                Vector3 topCenter = baseCenter + Vector3.up * Mathf.Abs(currentOffsetY) * Mathf.Sign(currentOffsetY);

                int segments = 30;
                float angleStep = 360f / segments;

                for (int i = 0; i < segments; i++)
                {
                    float angle = i * angleStep * Mathf.Deg2Rad;
                    Vector3 basePoint = baseCenter + new Vector3(Mathf.Cos(angle) * spawnCircleRadius, 0, Mathf.Sin(angle) * spawnCircleRadius);
                    Vector3 nextBasePoint = baseCenter + new Vector3(Mathf.Cos((i + 1) * angleStep * Mathf.Deg2Rad) * spawnCircleRadius, 0, Mathf.Sin((i + 1) * angleStep * Mathf.Deg2Rad) * spawnCircleRadius);
                    Vector3 topPoint = topCenter + new Vector3(Mathf.Cos(angle) * spawnCircleRadius, 0, Mathf.Sin(angle) * spawnCircleRadius);
                    Vector3 nextTopPoint = topCenter + new Vector3(Mathf.Cos((i + 1) * angleStep * Mathf.Deg2Rad) * spawnCircleRadius, 0, Mathf.Sin((i + 1) * angleStep * Mathf.Deg2Rad) * spawnCircleRadius);

                    Handles.DrawLine(basePoint, nextBasePoint);
                    Handles.DrawLine(topPoint, nextTopPoint);
                    Handles.DrawLine(basePoint, topPoint);
                }

                Vector3 arrowDirection = currentOffsetY > 0 ? Vector3.up : Vector3.down;
                Vector3 arrowPosition = topCenter + arrowDirection * 0.1f;
                float arrowSize = 1f;

                Handles.color = Color.yellow;
                Handles.ArrowHandleCap(0, arrowPosition, Quaternion.LookRotation(arrowDirection), arrowSize, EventType.Repaint);
            }
        }

        private void ConfigureShadows(GameObject obj, MeshData data)
        {
            if (obj == null) return;
            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.shadowCastingMode = data.castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                // Imposta anche la ricezione delle ombre se la proprietà esiste
#if UNITY_2020_1_OR_NEWER
                r.receiveShadows = data.receiveShadows;
#endif
            }
        }

        private void ConfigureStaticFlags(GameObject obj, MeshData.StaticFlags flags)
        {
            if (obj == null) return;
            GameObjectUtility.SetStaticEditorFlags(obj, GameObjectUtility.GetStaticEditorFlags(obj) | ConvertStaticFlags(flags));
            foreach (Transform child in obj.GetComponentsInChildren<Transform>())
            {
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, GameObjectUtility.GetStaticEditorFlags(child.gameObject) | ConvertStaticFlags(flags));
            }
        }

        private StaticEditorFlags ConvertStaticFlags(MeshData.StaticFlags flags)
        {
            StaticEditorFlags unityFlags = 0;
            if (flags.HasFlag(MeshData.StaticFlags.BatchingStatic)) unityFlags |= StaticEditorFlags.BatchingStatic;
            if (flags.HasFlag(MeshData.StaticFlags.OccluderStatic)) unityFlags |= StaticEditorFlags.OccluderStatic;
            if (flags.HasFlag(MeshData.StaticFlags.OccludeeStatic)) unityFlags |= StaticEditorFlags.OccludeeStatic;
            if (flags.HasFlag(MeshData.StaticFlags.ReflectionProbeStatic)) unityFlags |= StaticEditorFlags.ReflectionProbeStatic;
            if (flags.HasFlag(MeshData.StaticFlags.LightmapStatic)) unityFlags |= StaticEditorFlags.ContributeGI;
            if (flags.HasFlag(MeshData.StaticFlags.ContributeGIStatic)) unityFlags |= StaticEditorFlags.ContributeGI;
            return unityFlags;
        }

        private Texture2D MakeBorderTex(int width, int height, Color borderColor, int borderThickness)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = new Color(0, 0, 0, 0);
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = (x < borderThickness)
                                 || (x >= width - borderThickness)
                                 || (y < borderThickness)
                                 || (y >= height - borderThickness);
                    if (isBorder)
                    {
                        pix[x + y * width] = borderColor;
                    }
                }
            }

            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}
#endif