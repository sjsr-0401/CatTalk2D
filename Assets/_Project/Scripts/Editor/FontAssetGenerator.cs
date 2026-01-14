using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;

namespace CatTalk2D.Editor
{
    /// <summary>
    /// TextMeshPro 폰트 에셋 생성 에디터 유틸리티
    /// 한글 자모 + 이모지 지원 (간소화 버전)
    /// </summary>
    public class FontAssetGenerator : EditorWindow
    {
        private TMP_FontAsset _baseFontAsset;
        private string _outputPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/";
        private string _fontName = "malgun SDF Extended";

        [MenuItem("Tools/CatTalk2D/Extend Korean Font (Simple)")]
        public static void ShowWindow()
        {
            GetWindow<FontAssetGenerator>("Font Extender");
        }

        private void OnGUI()
        {
            GUILayout.Label("한글 자모 + 이모지 폰트 확장", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _baseFontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField(
                "Base Font Asset",
                _baseFontAsset,
                typeof(TMP_FontAsset),
                false
            );

            EditorGUILayout.Space();
            GUILayout.Label("Output Settings", EditorStyles.boldLabel);
            _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);
            _fontName = EditorGUILayout.TextField("Font Name", _fontName);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "기존 폰트를 복사하고 Dynamic 모드로 설정합니다.\n" +
                "런타임에 필요한 문자(한글 자모, 이모지)가 자동으로 추가됩니다.\n\n" +
                "1. Base Font Asset에 'malgun SDF' 드래그\n" +
                "2. Create Extended Font Asset 클릭\n" +
                "3. 생성된 폰트를 ChatUI에 연결",
                MessageType.Info
            );

            EditorGUILayout.Space();

            GUI.enabled = _baseFontAsset != null;
            if (GUILayout.Button("Create Extended Font Asset", GUILayout.Height(40)))
            {
                CreateExtendedFontAsset();
            }
            GUI.enabled = true;

            EditorGUILayout.Space();
            if (GUILayout.Button("Find 'malgun SDF' Font"))
            {
                FindMalgunFont();
            }
        }

        private void FindMalgunFont()
        {
            string[] guids = AssetDatabase.FindAssets("malgun SDF t:TMP_FontAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _baseFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                Debug.Log($"[FontExtender] 맑은 고딕 폰트 발견: {path}");
            }
            else
            {
                Debug.LogWarning("[FontExtender] 'malgun SDF' 폰트를 찾을 수 없습니다.");
            }
        }

        private void CreateExtendedFontAsset()
        {
            if (_baseFontAsset == null)
            {
                EditorUtility.DisplayDialog("Error", "Base Font Asset을 선택해주세요!", "OK");
                return;
            }

            try
            {
                Debug.Log($"[FontExtender] 폰트 복사 시작: {_baseFontAsset.name}");

                // 출력 경로 확인 및 생성
                if (!Directory.Exists(_outputPath))
                {
                    Directory.CreateDirectory(_outputPath);
                }

                string assetPath = Path.Combine(_outputPath, $"{_fontName}.asset");

                // 기존 에셋이 있으면 삭제
                if (File.Exists(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }

                // 폰트 에셋 복사
                TMP_FontAsset newFontAsset = Object.Instantiate(_baseFontAsset);
                newFontAsset.name = _fontName;

                // Dynamic 모드로 설정 (중요!)
                newFontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

                // 새 에셋으로 저장
                AssetDatabase.CreateAsset(newFontAsset, assetPath);

                // Material도 복사
                if (_baseFontAsset.material != null)
                {
                    Material newMaterial = new Material(_baseFontAsset.material);
                    newMaterial.name = $"{_fontName} Material";

                    string materialPath = Path.Combine(_outputPath, $"{_fontName}.mat");
                    if (File.Exists(materialPath))
                    {
                        AssetDatabase.DeleteAsset(materialPath);
                    }

                    AssetDatabase.CreateAsset(newMaterial, materialPath);
                    newFontAsset.material = newMaterial;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[FontExtender] ✅ 폰트 생성 완료: {assetPath}");
                EditorUtility.DisplayDialog(
                    "Success",
                    $"Dynamic 폰트 에셋이 생성되었습니다!\n\n" +
                    $"경로: {assetPath}\n\n" +
                    "이 폰트는 런타임에 한글 자모와 이모지를 자동으로 추가합니다.\n" +
                    "ChatUI의 Message Font 필드에 연결해주세요.",
                    "OK"
                );

                // 생성된 폰트 선택
                Selection.activeObject = newFontAsset;
                EditorGUIUtility.PingObject(newFontAsset);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FontExtender] ❌ 폰트 생성 실패: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"폰트 생성 실패:\n{ex.Message}", "OK");
            }
        }
    }
}
