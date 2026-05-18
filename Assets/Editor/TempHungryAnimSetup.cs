using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 임시 에디터 스크립트 — Cat/Setup Hungry Animation 실행 후 삭제할 것
/// </summary>
public static class TempHungryAnimSetup
{
    [MenuItem("Cat/Setup Hungry Animation")]
    static void Setup()
    {
        // 1) 에셋 로드
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            "Assets/Animations/CatAnimator.controller");
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/Animations/Hungry.anim");

        if (controller == null) { Debug.LogError("CatAnimator.controller 를 찾을 수 없습니다."); return; }
        if (clip      == null) { Debug.LogError("Hungry.anim 을 찾을 수 없습니다."); return; }

        // 2) 파라미터 추가 (중복 방지 헬퍼)
        static bool HasParam(AnimatorController c, string name)
        {
            foreach (var p in c.parameters) if (p.name == name) return true;
            return false;
        }

        if (!HasParam(controller, "isLadder"))
            controller.AddParameter("isLadder", AnimatorControllerParameterType.Bool);

        // isHungry Bool 파라미터 추가 (중복 방지)
        if (!HasParam(controller, "isHungry"))
            controller.AddParameter("isHungry", AnimatorControllerParameterType.Bool);

        // 3) Base Layer StateMachine
        var sm = controller.layers[0].stateMachine;

        // 4) 기존 Idle 상태 참조
        AnimatorState idleState   = null;
        AnimatorState walkState   = null;
        AnimatorState runState    = null;
        AnimatorState hideState   = null;
        AnimatorState hungryState = null;
        AnimatorState ladderState = null;

        foreach (var s in sm.states)
        {
            switch (s.state.name)
            {
                case "Idle":   idleState   = s.state; break;
                case "Walk":   walkState   = s.state; break;
                case "Run":    runState    = s.state; break;
                case "Hide":   hideState   = s.state; break;
                case "Hungry": hungryState = s.state; break;
                case "ladder": ladderState = s.state; break;
            }
        }

        if (idleState == null) { Debug.LogError("Idle 상태를 찾을 수 없습니다."); return; }

        // 5) Hungry 상태 추가 (중복 방지)
        if (hungryState == null)
        {
            hungryState        = sm.AddState("Hungry");
            hungryState.motion = clip;
        }

        // ── 기존 중복 전환 제거 헬퍼 ─────────────────────────────────────────

        static bool HasTransitionTo(AnimatorState src, AnimatorState dst)
        {
            foreach (var t in src.transitions)
                if (t.destinationState == dst) return true;
            return false;
        }

        // 6) Idle → Run (isRunning=true, 즉시) ← Walk→Run 은 기존에 있으나 Idle→Run 누락
        if (runState != null && !HasTransitionTo(idleState, runState))
        {
            var t = idleState.AddTransition(runState);
            t.AddCondition(AnimatorConditionMode.If, 0, "isRunning");
            t.hasExitTime = false;
            t.duration    = 0;
        }

        // 7) Idle → Hungry (isHungry=true, 즉시)
        if (!HasTransitionTo(idleState, hungryState))
        {
            var t = idleState.AddTransition(hungryState);
            t.AddCondition(AnimatorConditionMode.If, 0, "isHungry");
            t.hasExitTime = false;
            t.duration    = 0;
        }

        // 8) Hungry → Idle (isHungry=false, 즉시)
        if (!HasTransitionTo(hungryState, idleState))
        {
            var t = hungryState.AddTransition(idleState);
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "isHungry");
            t.hasExitTime = false;
            t.duration    = 0;
        }

        // 9) Hungry → Walk (isWalking=true)
        if (walkState != null && !HasTransitionTo(hungryState, walkState))
        {
            var t = hungryState.AddTransition(walkState);
            t.AddCondition(AnimatorConditionMode.If, 0, "isWalking");
            t.hasExitTime = false;
            t.duration    = 0;
        }

        // 10) Hungry → Hide (isDucking=true)
        if (hideState != null && !HasTransitionTo(hungryState, hideState))
        {
            var t = hungryState.AddTransition(hideState);
            t.AddCondition(AnimatorConditionMode.If, 0, "isDucking");
            t.hasExitTime = false;
            t.duration    = 0;
        }

        // 11) isLadder 전환: Idle/Walk/Fall → ladder (isLadder=true), ladder → Idle (isLadder=false)
        if (ladderState != null)
        {
            // Idle → ladder
            if (!HasTransitionTo(idleState, ladderState))
            {
                var t = idleState.AddTransition(ladderState);
                t.AddCondition(AnimatorConditionMode.If, 0, "isLadder");
                t.hasExitTime = false; t.duration = 0;
            }
            // Walk → ladder
            if (walkState != null && !HasTransitionTo(walkState, ladderState))
            {
                var t = walkState.AddTransition(ladderState);
                t.AddCondition(AnimatorConditionMode.If, 0, "isLadder");
                t.hasExitTime = false; t.duration = 0;
            }
            // ladder → Idle (isLadder=false)
            if (!HasTransitionTo(ladderState, idleState))
            {
                var t = ladderState.AddTransition(idleState);
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "isLadder");
                t.hasExitTime = false; t.duration = 0;
            }
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✅ Hungry + Idle→Run + isLadder 설정 완료! 메뉴: Cat > Setup Hungry Animation");
        Debug.Log("⚠️  이 스크립트를 삭제하세요: Assets/Editor/TempHungryAnimSetup.cs");
    }
}
