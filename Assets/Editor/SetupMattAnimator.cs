using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public class SetupMattAnimator
{
    [MenuItem("Tools/设置 Matt 角色动画")]
    static void Setup()
    {
        // 1. 尝试从多个来源收集动画剪辑
        var allClips = new List<AnimationClip>();

        // 来源 A: Matt__Motion.Fbx
        CollectClipsFromFBX(allClips, "Matt__Motion t:Model", "Assets/Character/Matt");

        // 来源 B: Standard Assets humanoid 动画（高质量，推荐）
        CollectClipsFromFolder(allClips, "Assets/Standard Assets/Characters/ThirdPersonCharacter/Animation");

        // 来源 C: 从 FBX 内部加载动画剪辑（解决 LoadAssetAtPath 只返回第一个的问题）
        CollectClipsFromFBXInternal(allClips, "Assets/Standard Assets/Characters/ThirdPersonCharacter/Animation");

        if (allClips.Count == 0)
        {
            Debug.LogError(
                "[SetupMattAnimator] 没有找到任何动画剪辑！\n" +
                "请确保以下至少一项存在：\n" +
                "  1. Assets/Character/Matt/Matt__Motion.Fbx (需在 Inspector 中配置动画剪辑)\n" +
                "  2. Assets/Standard Assets/Characters/ThirdPersonCharacter/Animation/ (标准资源)");
            return;
        }

        // 打印所有可用动画
        Debug.Log($"[SetupMattAnimator] 共找到 {allClips.Count} 个动画剪辑:");
        foreach (var c in allClips)
            Debug.Log($"  - '{c.name}' ({c.length:F2}s) [{GetSourceName(c)}]");

        // 2. 分类动画
        AnimationClip idleClip = null, walkClip = null, runClip = null, jumpClip = null, landClip = null;

        // 两轮匹配: 第一轮精确匹配，第二轮宽松匹配
        // 第一轮: 精确匹配 (排除 crouch/slide 等变体)
        foreach (AnimationClip clip in allClips)
        {
            string name = clip.name.ToLower();
            // 精确 Idle: 必须包含 "idle" 但不能是 crouch/slide/jump
            if (idleClip == null && MatchName(name, "idle") && !MatchName(name, "crouch", "slide", "sneak", "jump"))
                idleClip = clip;
            else if (runClip == null && MatchName(name, "run", "sprint") && !MatchName(name, "crouch", "slide"))
                runClip = clip;
            else if (walkClip == null && MatchName(name, "walk", "forward") && !MatchName(name, "crouch", "slide", "left", "right"))
                walkClip = clip;
            // 跳跃起跳: 优先 HumanoidJumpUp / HumanoidIdleJumpUp
            else if (jumpClip == null && (MatchName(name, "jumpup", "idlejumpup") || (MatchName(name, "jump") && !name.Contains("fall") && !name.Contains("midair") && !name.Contains("left"))))
                jumpClip = clip;
            // 空中/下落: 优先较长的 midair
            else if (landClip == null && (name.Contains("midair") || name.Contains("fall")) && !name.Contains("left"))
                landClip = clip;
        }

        // 第二轮: 如果第一轮没匹配到，放宽条件
        if (idleClip == null) foreach (var c in allClips) if (c.name.ToLower().Contains("idle") && !c.name.ToLower().Contains("jump")) { idleClip = c; break; }
        if (walkClip == null) foreach (var c in allClips) if (c.name.ToLower().Contains("walk")) { walkClip = c; break; }
        if (runClip == null) foreach (var c in allClips) if (c.name.ToLower().Contains("run")) { runClip = c; break; }
        if (jumpClip == null) foreach (var c in allClips) if (c.name.ToLower().Contains("jump") && !c.name.ToLower().Contains("fall")) { jumpClip = c; break; }
        if (landClip == null) foreach (var c in allClips) if (c.name.ToLower().Contains("fall") || c.name.ToLower().Contains("land")) { landClip = c; break; }

        // Fallback
        if (walkClip == null) walkClip = idleClip;
        if (runClip == null && walkClip != null) runClip = walkClip;
        if (jumpClip == null) jumpClip = walkClip;
        if (landClip == null) landClip = idleClip;

        Debug.Log($"[SetupMattAnimator] 分配结果:");
        Debug.Log($"  Idle   → {idleClip?.name ?? "(无)"}");
        Debug.Log($"  Walk   → {walkClip?.name ?? "(无)"}");
        Debug.Log($"  Run    → {runClip?.name ?? "(无)"}");
        Debug.Log($"  Jump   → {jumpClip?.name ?? "(无)"}");
        Debug.Log($"  Land   → {landClip?.name ?? "(无)"}");

        // 3. 创建 Animator Controller
        string controllerPath = "Assets/Character/Matt/MattAnimatorController.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // 清除旧数据
        controller.parameters = new AnimatorControllerParameter[0];
        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        sm.states = new ChildAnimatorState[0];
        sm.anyStateTransitions = new AnimatorStateTransition[0];

        // 4. 参数
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Land", AnimatorControllerParameterType.Trigger);

        // 5. 状态 (增加 Fall/MidAir 状态)
        AnimatorState idleState = AddState(sm, "Idle", idleClip);
        AnimatorState walkState = AddState(sm, "Walk", walkClip);
        AnimatorState runState = AddState(sm, "Run", runClip);
        AnimatorState jumpState = AddState(sm, "Jump", jumpClip);
        AnimatorState fallState = AddState(sm, "Fall", landClip); // 下落/空中
        AnimatorState landState = AddState(sm, "Land", idleClip); // 着陆用 idle 动画

        sm.defaultState = idleState;

        // 6. 过渡
        float walkThreshold = 0.15f;
        float runThreshold = 4.5f;

        // Idle <-> Walk
        AddSpeedTransition(idleState, walkState, walkThreshold, 0.12f);
        AddSpeedTransition(walkState, idleState, -walkThreshold, 0.15f);

        // Walk <-> Run
        AddSpeedTransition(walkState, runState, runThreshold, 0.12f);
        AddSpeedTransition(runState, walkState, -runThreshold * 0.7f, 0.18f);

        // Idle <-> Run (direct)
        AddSpeedTransition(idleState, runState, runThreshold + 1f, 0.2f);
        AddSpeedTransition(runState, idleState, -walkThreshold * 0.5f, 0.25f);

        // Any State → Jump (起跳)
        AnimatorStateTransition anyToJump = sm.AddAnyStateTransition(jumpState);
        anyToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        anyToJump.duration = 0.05f;
        anyToJump.hasExitTime = false;
        anyToJump.canTransitionToSelf = false;

        // Jump → Fall (起跳后自动过渡到下落)
        AnimatorStateTransition jumpToFall = jumpState.AddTransition(fallState);
        jumpToFall.hasExitTime = true;
        jumpToFall.exitTime = 0.7f;
        jumpToFall.duration = 0.1f;
        jumpToFall.hasFixedDuration = false;

        // Jump → Land (如果还在地面)
        AnimatorStateTransition jumpToLandDirect = jumpState.AddTransition(landState);
        jumpToLandDirect.AddCondition(AnimatorConditionMode.If, 0, "Land");
        jumpToLandDirect.duration = 0.08f;
        jumpToLandDirect.hasExitTime = false;

        // Fall → Land (着陆)
        AnimatorStateTransition fallToLand = fallState.AddTransition(landState);
        fallToLand.AddCondition(AnimatorConditionMode.If, 0, "Land");
        fallToLand.duration = 0.08f;
        fallToLand.hasExitTime = false;

        // Land → Idle / Walk / Run
        AnimatorStateTransition landToIdle = landState.AddTransition(idleState);
        landToIdle.hasExitTime = true;
        landToIdle.exitTime = 0.5f;
        landToIdle.duration = 0.1f;

        AnimatorStateTransition landToWalk = landState.AddTransition(walkState);
        landToWalk.AddCondition(AnimatorConditionMode.Greater, 0.3f, "Speed");
        landToWalk.hasExitTime = true;
        landToWalk.exitTime = 0.3f;
        landToWalk.duration = 0.1f;

        AnimatorStateTransition landToRun = landState.AddTransition(runState);
        landToRun.AddCondition(AnimatorConditionMode.Greater, runThreshold * 0.7f, "Speed");
        landToRun.hasExitTime = true;
        landToRun.exitTime = 0.3f;
        landToRun.duration = 0.1f;

        AssetDatabase.SaveAssets();
        Debug.Log($"[SetupMattAnimator] 控制器已创建: {controllerPath}");

        // 7. 绑定 Player
        BindToPlayer(controller);
    }

    static void CollectClipsFromFBX(List<AnimationClip> clips, string searchFilter, string folder)
    {
        string[] guids = AssetDatabase.FindAssets(searchFilter, new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object obj in assets)
            {
                AnimationClip clip = obj as AnimationClip;
                if (clip != null && !clip.name.StartsWith("__preview__") && !ContainsClip(clips, clip))
                    clips.Add(clip);
            }
        }
    }

    /// <summary>
    /// 从 FBX 文件内部加载所有动画剪辑（包括子剪辑）
    /// </summary>
    static void CollectClipsFromFBXInternal(List<AnimationClip> clips, string folder)
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx") && !path.EndsWith(".FBX")) continue;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object obj in assets)
            {
                AnimationClip clip = obj as AnimationClip;
                if (clip != null && !clip.name.StartsWith("__preview__") && !ContainsClip(clips, clip))
                    clips.Add(clip);
            }
        }
    }

    static bool ContainsClip(List<AnimationClip> clips, AnimationClip clip)
    {
        foreach (var c in clips)
            if (c.name == clip.name && c.length == clip.length)
                return true;
        return false;
    }

    static void CollectClipsFromFolder(List<AnimationClip> clips, string folder)
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null) clips.Add(clip);
        }
    }

    static string GetSourceName(AnimationClip clip)
    {
        return AssetDatabase.GetAssetPath(clip);
    }

    static bool MatchName(string name, params string[] keywords)
    {
        foreach (string kw in keywords)
        {
            if (name.Contains(kw))
                return true;
        }
        return false;
    }

    static AnimatorState AddState(AnimatorStateMachine sm, string stateName, AnimationClip clip)
    {
        AnimatorState state = sm.AddState(stateName);
        state.writeDefaultValues = true;
        if (clip != null)
        {
            state.motion = clip;
            Debug.Log($"  [状态] {stateName} → '{clip.name}' ({clip.length:F2}s)");
        }
        else
        {
            Debug.LogWarning($"  [警告] 状态 '{stateName}' 无动画分配！");
        }
        return state;
    }

    static void AddSpeedTransition(AnimatorState from, AnimatorState to, float threshold, float duration)
    {
        AnimatorStateTransition trans = from.AddTransition(to);
        if (threshold > 0)
            trans.AddCondition(AnimatorConditionMode.Greater, threshold, "Speed");
        else
            trans.AddCondition(AnimatorConditionMode.Less, Mathf.Abs(threshold), "Speed");
        trans.duration = duration;
        trans.hasExitTime = false;
    }

    static void BindToPlayer(AnimatorController controller)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("player");
        if (player == null) player = GameObject.Find("Player");

        if (player == null)
        {
            Debug.LogWarning("[SetupMattAnimator] 找不到 Player。请手动将控制器拖给 Animator 组件。路径:\n  " +
                             AssetDatabase.GetAssetPath(controller));
            return;
        }

        Animator anim = null;
        Animator[] animators = player.GetComponentsInChildren<Animator>();
        foreach (Animator a in animators)
        {
            if (a.avatar != null) { anim = a; break; }
        }
        if (anim == null)
        {
            SkinnedMeshRenderer[] renderers = player.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers.Length > 0)
                anim = renderers[0].gameObject.AddComponent<Animator>();
            else
                anim = player.AddComponent<Animator>();
        }

        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;
        EditorUtility.SetDirty(anim);
        Debug.Log($"[SetupMattAnimator] 已绑定到: {anim.gameObject.name}");
    }
}
