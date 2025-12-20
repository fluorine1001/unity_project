// Assets/Editor/FMODEventsAutoSync.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[InitializeOnLoad]
static class FMODEventsAutoSync
{
    static FMODEventsAutoSync()
    {
        // 어셈블리 재로드가 끝난 직후 실행
        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        // 에디터가 켜질 때 즉시 실행
        EditorApplication.delayCall += () => { /* optional immediate run on editor open */ };
    }

    private static void OnAfterAssemblyReload()
    {
        // 안전을 위해 에디터 전용으로만 동작
        SyncAllInstancesToDefaults();
    }

    public static void SyncAllInstancesToDefaults()
    {
        int updatedCount = 0;

        // 1) 열려있는 씬의 모든 인스턴스 (활성/비활성 포함)
        var sceneInstances = Object.FindObjectsOfType<FMODEvents>(true);
        updatedCount += ApplyDefaultsToObjects(sceneInstances);

        // 2) 프로젝트의 프리팹 에셋들 중 FMODEvents 컴포넌트를 가진 것들을 찾아서 수정
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        var prefabPaths = prefabGuids.Select(AssetDatabase.GUIDToAssetPath);

        foreach (var path in prefabPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            var comp = prefab.GetComponent<FMODEvents>();
            if (comp == null) continue;

            // SerializedObject로 프리팹 에셋의 컴포넌트 값을 변경
            var so = new SerializedObject(comp);
            bool any = false;

            any |= TrySetStringProp(so, "scene1MusicPath", FMODEvents.Defaults.Scene1Music);
            any |= TrySetStringProp(so, "scene2MusicPath", FMODEvents.Defaults.Scene2Music);

            any |= TrySetStringProp(so, "bulletLaunchedPath", FMODEvents.Defaults.BulletLaunched);
            any |= TrySetStringProp(so, "bulletAcceleratedPath", FMODEvents.Defaults.BulletAccelerated);
            any |= TrySetStringProp(so, "bulletDeceleratedPath", FMODEvents.Defaults.BulletDecelerated);
            any |= TrySetStringProp(so, "playerDashPath", FMODEvents.Defaults.PlayerDash);

            any |= TrySetStringProp(so, "boxPushedPath", FMODEvents.Defaults.BoxPushed);
            any |= TrySetStringProp(so, "boxBrokenPath", FMODEvents.Defaults.BoxBroken);
            any |= TrySetStringProp(so, "holeFilledPath", FMODEvents.Defaults.HoleFilled);

            any |= TrySetStringProp(so, "menuPressedPath", FMODEvents.Defaults.MenuPressed);
            any |= TrySetStringProp(so, "menuClosedPath", FMODEvents.Defaults.MenuClosed);
            any |= TrySetStringProp(so, "tilesSelectedPath", FMODEvents.Defaults.TilesSelected);
            any |= TrySetStringProp(so, "tilesDroppedPath", FMODEvents.Defaults.TilesDropped);
            any |= TrySetStringProp(so, "tilesBlockedPath", FMODEvents.Defaults.TilesBlocked);

            if (any)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                updatedCount++;
            }
        }

        // 씬 인스턴스에 대해서 씬 더티 표시 및 적용
        foreach (var scene in UnityEditor.SceneManagement.EditorSceneManager.GetAllScenes())
        {
            if (scene.isDirty)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }

        if (updatedCount > 0)
            Debug.Log($"FMODEventsAutoSync: applied defaults to {updatedCount} prefab(s)/instance groups.");
    }

    private static int ApplyDefaultsToObjects(IEnumerable<FMODEvents> instances)
    {
        int count = 0;
        foreach (var inst in instances)
        {
            if (inst == null) continue;
            var so = new SerializedObject(inst);
            bool any = false;

            any |= TrySetStringProp(so, "scene1MusicPath", FMODEvents.Defaults.Scene1Music);
            any |= TrySetStringProp(so, "scene2MusicPath", FMODEvents.Defaults.Scene2Music);

            any |= TrySetStringProp(so, "bulletLaunchedPath", FMODEvents.Defaults.BulletLaunched);
            any |= TrySetStringProp(so, "bulletAcceleratedPath", FMODEvents.Defaults.BulletAccelerated);
            any |= TrySetStringProp(so, "bulletDeceleratedPath", FMODEvents.Defaults.BulletDecelerated);
            any |= TrySetStringProp(so, "playerDashPath", FMODEvents.Defaults.PlayerDash);

            any |= TrySetStringProp(so, "boxPushedPath", FMODEvents.Defaults.BoxPushed);
            any |= TrySetStringProp(so, "boxBrokenPath", FMODEvents.Defaults.BoxBroken);
            any |= TrySetStringProp(so, "holeFilledPath", FMODEvents.Defaults.HoleFilled);

            any |= TrySetStringProp(so, "menuPressedPath", FMODEvents.Defaults.MenuPressed);
            any |= TrySetStringProp(so, "menuClosedPath", FMODEvents.Defaults.MenuClosed);
            any |= TrySetStringProp(so, "tilesSelectedPath", FMODEvents.Defaults.TilesSelected);
            any |= TrySetStringProp(so, "tilesDroppedPath", FMODEvents.Defaults.TilesDropped);
            any |= TrySetStringProp(so, "tilesBlockedPath", FMODEvents.Defaults.TilesBlocked);

            if (any)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(inst);
                var scene = inst.gameObject.scene;
                if (scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(scene);
                count++;
            }
        }
        if (count > 0) AssetDatabase.SaveAssets();
        return count;
    }

    private static bool TrySetStringProp(SerializedObject so, string propName, string value)
    {
        var prop = so.FindProperty(propName);
        if (prop == null) return false;
        if (prop.stringValue == value) return false;
        prop.stringValue = value;
        return true;
    }
}
#endif
