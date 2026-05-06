using System;
using UnityEditor;
using UnityEngine;

namespace Bukyja.ScatterAll
{
    public class CategoryNameWindow : EditorWindow
    {
        private string categoryName = "NewCategory";
        private Action<string> onNameSelected;

        public static void Show(Action<string> onNameSelected)
        {
            var window = ScriptableObject.CreateInstance<CategoryNameWindow>();
            window.onNameSelected = onNameSelected;
            window.titleContent = new GUIContent("Enter Category Name");
            window.minSize = new Vector2(250, 100);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            GUILayout.Label("Enter a name for the new category:", EditorStyles.wordWrappedLabel);
            categoryName = EditorGUILayout.TextField("Category Name", categoryName);

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("OK"))
            {
                onNameSelected?.Invoke(categoryName);
                Close();
            }

            if (GUILayout.Button("Cancel"))
            {
                onNameSelected?.Invoke(null);
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}