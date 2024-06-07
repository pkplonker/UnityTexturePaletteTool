using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TextureEditor : EditorWindow
{
	private Texture2D texture;

	public Texture2D Texture
	{
		get => texture;
		private set
		{
			if (texture != value)
			{
				texture = value;
				MakeTextureReadable(Texture);
			}
		}
	}

	private const int DEFAULT_TEX_SIZE = 256;
	private Vector2Int textureSize = new(DEFAULT_TEX_SIZE, DEFAULT_TEX_SIZE);
	private Color selectedColor = Color.white;
	private int selectedX = -1;
	private int selectedY = -1;
	private const string DEFAULT_FILENAME = "TexturePalette";

	private const int ZOOM_SIZE = 32;
	private const int ZOOM_SCALE = 8;
	private int displayZoom = 2;

	private Rect textureRect;
	private Vector2 mousePos;

	[MenuItem("Window/TextureEditor &%t")]
	public static void ShowWindow() => GetWindow<TextureEditor>("Texture Editor");

	private void OnGUI()
	{
		textureSize = EditorGUILayout.Vector2IntField("Width and Height", textureSize);
		displayZoom = EditorGUILayout.IntSlider("Display Zoom", displayZoom, 1, 10);
		GUILayout.BeginHorizontal();
		selectedX = EditorGUILayout.IntField("X", selectedX);
		selectedY = EditorGUILayout.IntField("Y", selectedY);
		GUILayout.EndHorizontal();
		CreateTexture();
		DrawTexture();
		SaveTexture();

		if (textureRect.Contains(Event.current.mousePosition))
		{
			mousePos = Event.current.mousePosition;
			Repaint();
		}

		DrawZoomedPreview();
	}
	
	private void SaveTexture()
	{
		if (GUILayout.Button("Save Texture"))
		{
			SaveAsset();
		}
	}

	private void MakeTextureReadable(Texture2D texture)
	{
		try
		{
			string path = AssetDatabase.GetAssetPath(texture);
			TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
			if (importer != null)
			{
				importer.isReadable = true;
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
			}
		}
		catch (Exception e)
		{
			Debug.Log($"Err {e}");
		}
	}

	private void SaveAsset()
	{
		var bytes = Texture.EncodeToPNG();

		var path = EditorUtility.SaveFilePanel("Save Texture As PNG", "", DEFAULT_FILENAME, "png");
		if (!string.IsNullOrEmpty(path))
		{
			File.WriteAllBytes(path, bytes);
			Debug.Log($"Texture saved to {path}");
			AssetDatabase.Refresh();

			string relativePath = "Assets" + path.Substring(Application.dataPath.Length);

			AssetDatabase.ImportAsset(relativePath);
			Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
			Selection.activeObject = Texture;
		}
	}
	private void DrawTexture()
	{
		if (Texture != null)
		{
			GUILayout.Label("Texture:", EditorStyles.boldLabel);
			textureRect = GUILayoutUtility.GetRect(textureSize.x * displayZoom, textureSize.y * displayZoom);
			textureRect.width = textureSize.x * displayZoom;
			textureRect.height = textureSize.y * displayZoom;
			GUI.DrawTexture(textureRect, Texture, ScaleMode.ScaleToFit);
			HandlePixelSelection(textureRect);

			if (selectedX >= 0 && selectedY >= 0)
			{
				selectedColor = EditorGUILayout.ColorField("Selected Pixel Color", selectedColor);
				if (GUILayout.Button("Apply Color"))
				{
					ApplyColorToPixel();
				}
			}
		}
	}
	private void HandlePixelSelection(Rect textureRect)
	{
		Event evt = Event.current;
		if (evt.type == EventType.MouseDown && textureRect.Contains(evt.mousePosition))
		{
			Vector2 mousePos = evt.mousePosition;
			selectedX = Mathf.FloorToInt((mousePos.x - textureRect.x) / displayZoom);
			selectedY = Mathf.FloorToInt((mousePos.y - textureRect.y) / displayZoom);

			selectedColor = Texture.GetPixel(selectedX, selectedY);
			evt.Use();
		}
	}


	private void ApplyColorToPixel()
	{
		if (Texture != null && selectedX >= 0 && selectedY >= 0)
		{
			Texture.SetPixel(selectedX, selectedY, selectedColor);
			Texture.Apply();
		}
	}

	private void CreateTexture()
	{
		if (GUILayout.Button("Create Texture"))
		{
			Texture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGBA32, false);
			texture.filterMode = FilterMode.Point;
		}
	}

	private void DrawZoomedPreview()
	{
		if (textureRect.Contains(mousePos))
		{
			int texX = Mathf.FloorToInt((mousePos.x - textureRect.x) / displayZoom);
			int texY = Mathf.FloorToInt((mousePos.y - textureRect.y) / displayZoom);

			Rect zoomRect = new Rect(mousePos.x + 10, mousePos.y + 10, (ZOOM_SIZE / 2) * ZOOM_SCALE, (ZOOM_SIZE / 2) * ZOOM_SCALE);
			GUI.Box(zoomRect, GUIContent.none);

			for (int y = -ZOOM_SIZE / 4; y < ZOOM_SIZE / 4; y++)
			{
				for (int x = -ZOOM_SIZE / 4; x < ZOOM_SIZE / 4; x++)
				{
					int drawX = texX + x;
					int drawY = texY + y;
					if (drawX >= 0 && drawX < Texture.width && drawY >= 0 && drawY < Texture.height)
					{
						Color color = Texture.GetPixel(drawX, drawY);
						EditorGUI.DrawRect(
							new Rect(zoomRect.x + (x + ZOOM_SIZE / 4) * ZOOM_SCALE,
								zoomRect.y + (y + ZOOM_SIZE / 4) * ZOOM_SCALE, ZOOM_SCALE, ZOOM_SCALE), color);
					}
				}
			}

			Handles.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Dark grey
			Handles.DrawSolidRectangleWithOutline(zoomRect, Color.clear, new Color(0.3f, 0.3f, 0.3f, 1f));
		}
	}
}
