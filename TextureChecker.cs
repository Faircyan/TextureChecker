#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

public class TextureChecker : EditorWindow
{
    private Dictionary<string, List<Texture2D>> _textureHashes;
    private const int TextureSize = 100;
    private const int InspectorSize = 300;
    private static readonly Vector2 DefaultWindowSize = new Vector2(800, 600);

    private Vector2 _scrollPosition;
    private string _activeHash;

    private readonly List<string> _ignorePaths = new() { "Packages/" };

    [MenuItem("Tools/Texture Checker")]
    public static void ShowWindow()
    {
        var window = GetWindow<TextureChecker>();
        window.titleContent = new GUIContent("Texture Checker");
        window.minSize = DefaultWindowSize;
        window.Show();
    }

    private void OnEnable()
    {
        _textureHashes = new Dictionary<string, List<Texture2D>>();
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Find Duplicate Textures")) FindDuplicateTextures();
        GUILayout.Label($"Total Duplicate Groups Found: {_textureHashes.Count}");
        GUILayout.BeginHorizontal();
        DrawTextures();

        GUILayout.Box("", GUILayout.Width(5), GUILayout.ExpandHeight(true));
        if (!string.IsNullOrEmpty(_activeHash)) DrawInspector();
        GUILayout.EndHorizontal();
    }

    private void DrawInspector()
    {
        GUILayout.BeginVertical();
        var hash = _textureHashes[_activeHash];
        GUILayout.Label(hash[0], GUILayout.Width(200), GUILayout.Height(200));
        foreach (var texture in hash)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(texture.name, GUILayout.Width(200), GUILayout.Height(30)))
            {
                Selection.activeObject = texture;
                EditorGUIUtility.PingObject(texture);
            }

            var deleteIcon = EditorGUIUtility.IconContent("TreeEditor.Trash");
            if (GUILayout.Button(deleteIcon, GUILayout.Width(30), GUILayout.Height(30)))
            {
                var path = AssetDatabase.GetAssetPath(texture);
                AssetDatabase.DeleteAsset(path);
                FindDuplicateTextures();
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
    }

    private void DrawTextures()
    {
        var windowWidth = position.width;
        var itemsPerRow = (int)((windowWidth - InspectorSize) / TextureSize);

        GUILayout.BeginVertical();
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
        for (var i = 0; i < _textureHashes.Count; i++)
        {
            if (i % itemsPerRow == 0) GUILayout.BeginHorizontal();
            DrawDuplicateTexture(_textureHashes.ElementAt(i).Value);
            if (i % itemsPerRow == itemsPerRow - 1 || i == _textureHashes.Count - 1) GUILayout.EndHorizontal();
        }
            
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawDuplicateTexture(List<Texture2D> hashValue)
    {
        var texture = hashValue[0];

        var style = new GUIStyle(GUI.skin.button);
        style.normal.background = texture;
        style.imagePosition = ImagePosition.ImageOnly;
        
        if (GUILayout.Button(new GUIContent(String.Empty, texture), GUILayout.Width(TextureSize), GUILayout.Height(TextureSize)))
        {
            _activeHash = GetTextureHash(texture);
        }
    }

    private void FindDuplicateTextures()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D");
        var newHashes = new Dictionary<string, List<Texture2D>>();
        _activeHash = string.Empty;
        _textureHashes.Clear();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (_ignorePaths.Any(path.Contains)) continue;
            
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (texture == null) continue;
            var textureHash = GetTextureHash(texture);

            if (!newHashes.ContainsKey(textureHash)) newHashes[textureHash] = new List<Texture2D>();

            newHashes[textureHash].Add(texture);
        }

        foreach (var textureHash in newHashes.ToList())
        {
            if (textureHash.Value.Count > 1)
            {
                _textureHashes.Add(textureHash.Key, textureHash.Value);
            }
        }
    }

    private string GetTextureHash(Texture2D texture)
    {
        var pixels = texture.GetRawTextureData();
        return Convert.ToBase64String(MD5.Create().ComputeHash(pixels));
    }
}
#endif