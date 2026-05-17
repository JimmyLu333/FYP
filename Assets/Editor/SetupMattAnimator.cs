using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SetupMattAnimator
{
    [MenuItem("Tools/设置 Matt 角色动画")]
    static void Setup()
    {
        // 1. 找到 Matt__Motion 里的动画剪辑
        string[] motionGuids = AssetDatabase.FindAssets("Matt__Motion t:Model", new[] { "Assets/Character/Matt" });
        if (motionGuids.Length == 0)
        {
            Debug.LogError("找不到 Matt__Motion.Fbx，请确保文件存在且已导入");
            return;
        }

        string motionPath = AssetDatabase.GUIDToAssetPath(motionGuids[0]);
        Object[] motionAssets = AssetDatabase.LoadAllAssetsAtPath(motionPath);

        // 收集所有 AnimationClip
        var clips = new System.Collections.Generic.List<AnimationClip>();
        foreach (Object obj in motionAssets)
        {
            AnimationClip clip = obj as AnimationClip;
            if (clip != null && !clip.name.StartsWith("__preview__"))
            {
                clips.Add(clip);
                Debug.Log($"找到动画剪辑: {clip.name}");
            }
        }

        if (clips.Count == 0)
        {
            Debug.LogError("Matt__Motion 中没有找到动画剪辑！可能需要先在 Inspector 中点击导入。");
            return;
        }

        // 2. 创建 Animator Controller
        string controllerPath = "Assets/Character/Matt/MattAnimatorController.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        // 添加参数
        AnimatorControllerParameter[] existingParams = controller.parameters;
        bool hasSpeed = false, hasIsGrounded = false;
        foreach (var p in existingParams)
        {
            if (p.name == "Speed") hasSpeed = true;
            if (p.name == "IsGrounded") hasIsGrounded = true;
        }
        if (!hasSpeed) controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        if (!hasIsGrounded) controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);

        // 3. 创建状态机
        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // 清除默认状态（保留已有状态）
        // 添加动画状态
        AnimatorState idleState = null;
        AnimatorState walkState = null;
        AnimatorState runState = null;

        foreach (AnimationClip clip in clips)
        {
            string name = clip.name.ToLower();
            AnimatorState state = sm.AddState(clip.name);
            state.motion = clip;

            if (name.Contains("idle") || name.Contains("stand"))
            {
                idleState = state;
                sm.defaultState = state;
            }
            else if (name.Contains("walk"))
            {
                walkState = state;
            }
            else if (name.Contains("run") || name.Contains("sprint"))
            {
                runState = state;
            }
        }

        // 如果没有找到 idle，用第一个动画
        if (idleState == null && clips.Count > 0)
        {
            idleState = sm.states[0].state;
            sm.defaultState = idleState;
        }

        // 4. 创建过渡
        if (idleState != null && walkState != null)
        {
            // Idle -> Walk
            AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.duration = 0.2f;

            // Walk -> Idle
            AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.duration = 0.2f;
        }

        if (walkState != null && runState != null)
        {
            // Walk -> Run
            AnimatorStateTransition walkToRun = walkState.AddTransition(runState);
            walkToRun.AddCondition(AnimatorConditionMode.Greater, 5f, "Speed");
            walkToRun.duration = 0.2f;

            // Run -> Walk
            AnimatorStateTransition runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.Less, 5f, "Speed");
            runToWalk.duration = 0.2f;
        }

        if (idleState != null && runState != null)
        {
            // Idle -> Run
            AnimatorStateTransition idleToRun = idleState.AddTransition(runState);
            idleToRun.AddCondition(AnimatorConditionMode.Greater, 5f, "Speed");
            idleToRun.duration = 0.25f;

            // Run -> Idle
            AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            runToIdle.duration = 0.25f;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Matt 动画控制器创建完成！共 {clips.Count} 个动画剪辑，路径: {controllerPath}");

        // 5. 自动给 Player 设置 Animator
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Animator anim = player.GetComponentInChildren<Animator>();
            if (anim == null)
            {
                // 找 Matt 模型的 SkinnedMeshRenderer 所在对象
                Animator[] animators = player.GetComponentsInChildren<Animator>();
                if (animators.Length > 0) anim = animators[0];
            }
            if (anim == null)
            {
                // 给有 SkinnedMeshRenderer 的子对象加 Animator
                SkinnedMeshRenderer[] renderers = player.GetComponentsInChildren<SkinnedMeshRenderer>();
                if (renderers.Length > 0)
                {
                    anim = renderers[0].gameObject.AddComponent<Animator>();
                }
                else
                {
                    anim = player.AddComponent<Animator>();
                }
            }
            anim.runtimeAnimatorController = controller;
            Debug.Log($"已将 Animator Controller 绑定到 {anim.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("找不到 Player，请手动将 MattAnimatorController 拖到 Matt 模型的 Animator 组件上");
        }
    }
}
