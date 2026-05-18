using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class AssignAnimatorOnLoad
{
    static AssignAnimatorOnLoad()
    {
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/CatAnimator.controller");
        if (ctrl == null) return;

        var player = GameObject.Find("Player");
        if (player == null) return;

        var anim = player.GetComponent<Animator>();
        if (anim == null) return;

        if (anim.runtimeAnimatorController == ctrl) return;

        anim.runtimeAnimatorController = ctrl;
        EditorUtility.SetDirty(player);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Setup] CatAnimator.controller → Player 연결 완료");
    }
}
