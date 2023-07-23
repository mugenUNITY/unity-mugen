using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityMugen;
using UnityMugen.Animations;
using UnityMugen.Collections;
using UnityMugen.Combat;
using UnityMugen.Commands;

public class FullDebugEditorWindow : EditorWindow
{

    public static FullDebugEditorWindow editorWindow;

    LauncherEngine Launcher => LauncherEngine.Inst;
    FightEngine Engine => BattleActive();

    Vector2 m_scrollPos;
    bool m_players, m_helpers, m_explods, m_projectiles;
    bool m_assertSpecial, m_shake, m_match, m_pause, m_superPause;
    bool m_intVars, m_floatVars, m_sysIntVars, m_sysFloatVars;
    bool m_bind, m_animation, m_assertion, m_cmdBufferTime;

    GUILayoutOption[] m_optionsVars = new GUILayoutOption[] { GUILayout.Width(150), GUILayout.MinWidth(100)/*,GUILayout.ExpandWidth(true)*/};

    Dictionary<long, bool> m_foldoutEntities = new Dictionary<long, bool>();
    Dictionary<long, bool> m_foldoutAnimations = new Dictionary<long, bool>();
    Dictionary<long, bool> m_foldoutCmd = new Dictionary<long, bool>();
    Dictionary<long, bool> m_toggleActiveCmd = new Dictionary<long, bool>();

    public string nameEntity;
    bool searchBy;
    bool pauseWhenFindId;

    public string id;
    private int? ID => IDValue();

    int? IDValue()
    {
        if (!string.IsNullOrEmpty(id))
        {
            if (int.TryParse(id, out int number))
            {
                return number;
            }
            else
            {
                id = "";
                return null;
            }
        }
        return null;
    }

    [MenuItem("UnityMugen/Full Debug")]
    static void Init()
    {
        editorWindow = EditorWindow.GetWindow<FullDebugEditorWindow>(false, "Full Debug", true);
        editorWindow.minSize = new Vector2(574, 533);
        editorWindow.Show();
    }


    FightEngine BattleActive()
    {
        return Launcher != null &&
            Launcher.mugen != null &&
            Launcher.mugen.BattleActive ? Launcher.mugen.Engine : null;
    }

    void OnGUI()
    {
        SerializedObject serializedObject = new SerializedObject(this);

        EditorGUILayout.BeginVertical("ObjectFieldThumb");
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUI.BeginChangeCheck();

                EditorGUIUtility.labelWidth = 30;
                id = EditorGUILayout.TextField("ID:", id);

                GUILayout.Space(15);

                EditorGUIUtility.labelWidth = 90;
                nameEntity = EditorGUILayout.TextField("Entity Name:", nameEntity, GUILayout.Width(90 + 200));

                if (EditorGUI.EndChangeCheck())
                {
                    searchBy = (ID != null || !string.IsNullOrEmpty(nameEntity));
                }

                GUILayout.Space(15);

                EditorGUIUtility.labelWidth = 120;
                EditorGUI.BeginChangeCheck();
                pauseWhenFindId = EditorGUILayout.Toggle("Pause on finding ID:", pauseWhenFindId);
                if (EditorGUI.EndChangeCheck())
                {
                    if (!pauseWhenFindId) EditorApplication.isPaused = false;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                GUIStyle StateFieldLabelColorGreen = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = new GUIStyleState() { textColor = Color.green }
                };

                GUILayout.FlexibleSpace();
                EditorGUIUtility.labelWidth = 90;
                EditorGUILayout.LabelField(searchBy ? "Searching By Parameter" : "", StateFieldLabelColorGreen);
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();





        m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);



        if (Engine != null)
        {
            if (searchBy)
                SearchBy();
            else
                SearchAll();
        }
        else
        {
            GUIStyle StateFieldLabelColorRed = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = new GUIStyleState() { textColor = Color.yellow, }
            };
            StateFieldLabelColorRed.fontSize = 18;

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();
            //EditorGUIUtility.labelWidth = 70;
            //EditorGUILayout.LabelField("NO FIGHTING", StateFieldLabelColorRed);
            //EditorGUILayout.LabelField("Start a fight to debug.");
            ShowNotification(new GUIContent("Start a fight to debug."));
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();
        EditorGUIUtility.labelWidth = 60;
        EditorGUILayout.LabelField("Version: 0.0.1", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
        Repaint();
    }

    void SearchBy()
    {
        foreach (Entity entity in Engine.Entities)
        {
            if ((ID.HasValue && entity.Id == ID.Value) || entity.NameSearch == nameEntity)
            {
                if (pauseWhenFindId) EditorApplication.isPaused = true;

                if (!m_foldoutEntities.ContainsKey(entity.UniqueID))
                    m_foldoutEntities.Add(entity.UniqueID, false);

                EditorGUILayout.BeginVertical("ObjectFieldThumb");
                {
                    m_foldoutEntities[entity.UniqueID] = EditorGUILayout.Foldout(m_foldoutEntities[entity.UniqueID], entity.name, true, "Foldout");
                    if (m_foldoutEntities[entity.UniqueID])
                    {
                        if (entity.typeEntity == TypeEntity.Player)
                            CharacterInfo(entity as Player);
                        else if (entity.typeEntity == TypeEntity.Helper)
                            HelperInfo(entity as Helper);
                        else if (entity.typeEntity == TypeEntity.Explod)
                            ExplodInfo(entity as Explod);
                        else if (entity.typeEntity == TypeEntity.Projectile)
                            ProjectileInfo(entity as Projectile);
                    }
                }
                EditorGUILayout.EndVertical();
            }

        }

        OtherInfo();
    }

    void SearchAll()
    {
        EditorGUILayout.BeginVertical("ObjectFieldThumb");
        {
            m_players = EditorGUILayout.Foldout(m_players, "Players", true, "Foldout");
            if (m_players)
            {
                foreach (Entity entity in Engine.Entities)
                {
                    if (entity.typeEntity == TypeEntity.Player)
                    {
                        if (!m_foldoutEntities.ContainsKey(entity.UniqueID))
                            m_foldoutEntities.Add(entity.UniqueID, false);

                        EditorGUILayout.BeginVertical("ObjectFieldThumb");
                        {
                            m_foldoutEntities[entity.UniqueID] = EditorGUILayout.Foldout(m_foldoutEntities[entity.UniqueID], entity.name, true, "Foldout");
                            if (m_foldoutEntities[entity.UniqueID])
                            {
                                CharacterInfo(entity as Player);
                            }
                        }
                        EditorGUILayout.EndVertical();

                    }
                }
            }
        }
        EditorGUILayout.EndVertical();


        EditorGUILayout.BeginVertical("ObjectFieldThumb");
        {
            m_helpers = EditorGUILayout.Foldout(m_helpers, "Helpers", true, "Foldout");
            if (m_helpers)
            {
                foreach (Entity entity in Engine.Entities)
                {
                    if (entity.typeEntity == TypeEntity.Helper)
                    {
                        if (!m_foldoutEntities.ContainsKey(entity.UniqueID))
                            m_foldoutEntities.Add(entity.UniqueID, false);

                        EditorGUILayout.BeginVertical("ObjectFieldThumb");
                        {
                            m_foldoutEntities[entity.UniqueID] = EditorGUILayout.Foldout(m_foldoutEntities[entity.UniqueID], entity.name, true, "Foldout");
                            if (m_foldoutEntities[entity.UniqueID])
                            {
                                HelperInfo(entity as Helper);
                            }
                        }
                        EditorGUILayout.EndVertical();

                    }
                }
            }
        }
        EditorGUILayout.EndVertical();


        EditorGUILayout.BeginVertical("ObjectFieldThumb");
        {
            m_explods = EditorGUILayout.Foldout(m_explods, "Explods", true, "Foldout");
            if (m_explods)
            {
                foreach (Entity entity in Engine.Entities)
                {
                    if (entity.typeEntity == TypeEntity.Explod)
                    {
                        if (!m_foldoutEntities.ContainsKey(entity.UniqueID))
                            m_foldoutEntities.Add(entity.UniqueID, false);

                        EditorGUILayout.BeginVertical("ObjectFieldThumb");
                        {
                            m_foldoutEntities[entity.UniqueID] = EditorGUILayout.Foldout(m_foldoutEntities[entity.UniqueID], entity.name, true, "Foldout");
                            if (m_foldoutEntities[entity.UniqueID])
                            {
                                ExplodInfo(entity as Explod);
                            }
                        }
                        EditorGUILayout.EndVertical();

                    }
                }
            }
        }
        EditorGUILayout.EndVertical();


        EditorGUILayout.BeginVertical("ObjectFieldThumb");
        {
            m_projectiles = EditorGUILayout.Foldout(m_projectiles, "Projectiles", true, "Foldout");
            if (m_projectiles)
            {
                foreach (Entity entity in Engine.Entities)
                {
                    if (entity.typeEntity == TypeEntity.Projectile)
                    {
                        if (!m_foldoutEntities.ContainsKey(entity.UniqueID))
                            m_foldoutEntities.Add(entity.UniqueID, false);

                        EditorGUILayout.BeginVertical("ObjectFieldThumb");
                        {
                            m_foldoutEntities[entity.UniqueID] = EditorGUILayout.Foldout(m_foldoutEntities[entity.UniqueID], entity.name, true, "Foldout");
                            if (m_foldoutEntities[entity.UniqueID])
                            {
                                ProjectileInfo(entity as Projectile);
                            }
                        }
                        EditorGUILayout.EndVertical();

                    }
                }
            }
        }
        EditorGUILayout.EndVertical();


        OtherInfo();
    }

    void CharacterInfo(Player mainPlayer)
    {
        EditorGUILayout.ObjectField(mainPlayer, typeof(Player), true);

        EditorGUILayout.BeginVertical("ObjectFieldThumb");
        {

            CommonsData(mainPlayer as Entity);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("PaletteNo: ");
                EditorGUILayout.LabelField(mainPlayer.PaletteNumber.ToString());
                EditorGUILayout.LabelField("int");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Power: ");
                EditorGUILayout.LabelField(mainPlayer.Power.ToString());
                EditorGUILayout.LabelField("float");
            }
            EditorGUILayout.EndHorizontal();

            EntityData(mainPlayer as Entity);
            CharacterData(mainPlayer as Character);
            
        }
        EditorGUILayout.EndVertical();
    }

    void HelperInfo(Helper helper)
    {
        CommonsData(helper as Entity);
        EntityData(helper as Entity);
        CharacterData(helper as Character);
    }

    void ExplodInfo(Explod explod)
    {
        CommonsData(explod as Entity);
        EntityData(explod as Entity);
    }

    void ProjectileInfo(Projectile projectile)
    {
        CommonsData(projectile as Entity);
        EntityData(projectile as Entity);
    }

    void CommonsData(Entity entity)
    {
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("Name");
            EditorGUILayout.LabelField("Value");
            EditorGUILayout.LabelField("Type");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("Id: ");
            EditorGUILayout.LabelField(entity.Id.ToString());
            EditorGUILayout.LabelField("int");
        }
        EditorGUILayout.EndHorizontal();
    }

    void EntityData(Entity entity)
    {
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("Position: ");
            EditorGUILayout.LabelField(entity.CurrentLocation.ToString());
            EditorGUILayout.LabelField("Vector2");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("Velocity: ");
            EditorGUILayout.LabelField(entity.CurrentVelocity.ToString());
            EditorGUILayout.LabelField("Vector2");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("CurrentFacing: ");
            EditorGUILayout.LabelField(entity.CurrentFacing.ToString());
            EditorGUILayout.LabelField("Type");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("CurrentFlip: ");
            EditorGUILayout.LabelField(entity.CurrentFlip.ToString());
            EditorGUILayout.LabelField("Type");
        }
        EditorGUILayout.EndHorizontal();

    }

    void CharacterData(Character character)
    {
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("Life: ");
            EditorGUILayout.LabelField(character.Life.ToString());
            EditorGUILayout.LabelField("float");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("JugglePoints: ");
            EditorGUILayout.LabelField(character.JugglePoints.ToString());
            EditorGUILayout.LabelField("int");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("StateType: ");
            EditorGUILayout.LabelField(character.StateType.ToString());
            EditorGUILayout.LabelField("Type");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("Physics: ");
            EditorGUILayout.LabelField(character.Physics.ToString());
            EditorGUILayout.LabelField("Type");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("MoveType: ");
            EditorGUILayout.LabelField(character.MoveType.ToString());
            EditorGUILayout.LabelField("Type");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("PlayerControl: ");
            EditorGUILayout.LabelField(character.PlayerControl.ToString());
            EditorGUILayout.LabelField("Type");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("hit.pause.time: ");
            EditorGUILayout.LabelField(character.OffensiveInfo.HitPauseTime.ToString());
            EditorGUILayout.LabelField("int");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("camera.follow.x: ");
            EditorGUILayout.LabelField(character.CameraFollowX.ToString());
            EditorGUILayout.LabelField("bool");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("camera.follow.y: ");
            EditorGUILayout.LabelField(character.CameraFollowY.ToString());
            EditorGUILayout.LabelField("bool");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("push.checking: ");
            EditorGUILayout.LabelField(character.PushFlag.ToString());
            EditorGUILayout.LabelField("bool");
        }
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("StateNo: ");
            EditorGUILayout.LabelField(character.StateManager.CurrentState.number.ToString());
            EditorGUILayout.LabelField("int");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("PrevStateNo: ");
            EditorGUILayout.LabelField(character.StateManager.PreviousState != null ? character.StateManager.PreviousState.number.ToString() : "None");
            EditorGUILayout.LabelField("int");
        }
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("StateTime: ");
            EditorGUILayout.LabelField(character.StateManager.StateTime.ToString());
            EditorGUILayout.LabelField("int");
        }
        EditorGUILayout.EndHorizontal();

        AnimationManager(character.AnimationManager);

        EditorGUILayout.BeginVertical("ObjectFieldThumb");
        {
            m_bind = EditorGUILayout.Foldout(m_bind, "Bind:", true, "Foldout");
            if (m_bind)
            {
                if (character.Bind != null && character.Bind.BindTo != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Name: ");
                        EditorGUILayout.LabelField(character.Bind.BindTo.name.ToString());
                        EditorGUILayout.LabelField("string");
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Time: ");
                        EditorGUILayout.LabelField(character.Bind.Time.ToString());
                        EditorGUILayout.LabelField("int");
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Facing: ");
                        EditorGUILayout.LabelField(character.Bind.FacingFlag.ToString());
                        EditorGUILayout.LabelField("int");
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Position: ");
                        EditorGUILayout.LabelField(character.Bind.Offset.ToString());
                        EditorGUILayout.LabelField("vector2");
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField("No Bind ");
                }
            }
        }
        EditorGUILayout.EndVertical();



        EditorGUILayout.BeginVertical("ObjectFieldThumb");
        {
            m_assertion = EditorGUILayout.Foldout(m_assertion, "Assertions:", true, "Foldout");
            if (m_assertion)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("NoStandingGuard:");
                EditorGUILayout.LabelField(character.Assertions.NoStandingGuard.ToString());
                EditorGUILayout.LabelField("bool");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("NoCrouchingGuard:");
                EditorGUILayout.LabelField(character.Assertions.NoCrouchingGuard.ToString());
                EditorGUILayout.LabelField("bool");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("NoAirGuard:");
                EditorGUILayout.LabelField(character.Assertions.NoAirGuard.ToString());
                EditorGUILayout.LabelField("bool");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("NoAutoTurn:");
                EditorGUILayout.LabelField(character.Assertions.NoAutoTurn.ToString());
                EditorGUILayout.LabelField("bool");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("NoShadow:");
                EditorGUILayout.LabelField(character.Assertions.NoShadow.ToString());
                EditorGUILayout.LabelField("bool");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("NoJuggleCheck:");
                EditorGUILayout.LabelField(character.Assertions.NoJuggleCheck.ToString());
                EditorGUILayout.LabelField("bool");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("NoWalk:");
                EditorGUILayout.LabelField(character.Assertions.NoWalk.ToString());
                EditorGUILayout.LabelField("bool");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("UnGuardable:");
                EditorGUILayout.LabelField(character.Assertions.UnGuardable.ToString());
                EditorGUILayout.LabelField("bool");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Invisible:");
                EditorGUILayout.LabelField(character.Assertions.Invisible.ToString());
                EditorGUILayout.LabelField("bool");
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndVertical();

        Cmd(character.CommandManager);

        ShowVars<int>("Vars:", character.Variables.IntegerVariables, ref m_intVars);
        ShowVars<float>("FloatVars:", character.Variables.FloatVariables, ref m_floatVars);
        ShowVars<int>("SysVars:", character.Variables.SystemIntegerVariables, ref m_sysIntVars);
        ShowVars<float>("SysFloatVars:", character.Variables.SystemFloatVariables, ref m_sysFloatVars);
    }

    void OtherInfo()
    {

        EditorGUILayout.BeginVertical("ObjectFieldThumb");
        {
            m_assertSpecial = EditorGUILayout.Foldout(m_assertSpecial, "Assert Special:", true, "Foldout");
            if (m_assertSpecial)
            {
                AssertSpecial(Engine.Assertions);
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("ObjectFieldThumb");
        {
            m_shake = EditorGUILayout.Foldout(m_shake, "Environment Shake:", true, "Foldout");
            if (m_shake)
            {
                EnvShake(Engine.EnvironmentShake);
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("ObjectFieldThumb");
        {
            m_match = EditorGUILayout.Foldout(m_match, "Match:", true, "Foldout");
            if (m_match)
            {
                Match(Engine);
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("ObjectFieldThumb");
        {
            m_pause = EditorGUILayout.Foldout(m_pause, "Pause:", true, "Foldout");
            if (m_pause)
            {
                Pause(Engine.Pause);
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("ObjectFieldThumb");
        {
            m_superPause = EditorGUILayout.Foldout(m_superPause, "Super Pause:", true, "Foldout");
            if (m_superPause)
            {
                Pause(Engine.SuperPause);
            }
        }
        EditorGUILayout.EndVertical();

    }

    void AssertSpecial(EngineAssertions assertions)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("NoKOSound:");
        EditorGUILayout.LabelField(assertions.NoKOSound.ToString());
        EditorGUILayout.LabelField("bool");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("NoKOSlow:");
        EditorGUILayout.LabelField(assertions.NoKOSlow.ToString());
        EditorGUILayout.LabelField("bool");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("GlobalNoShadow:");
        EditorGUILayout.LabelField(assertions.GlobalNoShadow.ToString());
        EditorGUILayout.LabelField("bool");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("NoMusic:");
        EditorGUILayout.LabelField(assertions.NoMusic.ToString());
        EditorGUILayout.LabelField("bool");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("TimerFreeze:");
        EditorGUILayout.LabelField(assertions.TimerFreeze.ToString());
        EditorGUILayout.LabelField("bool");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Intro:");
        EditorGUILayout.LabelField(assertions.Intro.ToString());
        EditorGUILayout.LabelField("bool");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("NoBarDisplay:");
        EditorGUILayout.LabelField(assertions.NoBarDisplay.ToString());
        EditorGUILayout.LabelField("bool");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("WinPose:");
        EditorGUILayout.LabelField(assertions.WinPose.ToString());
        EditorGUILayout.LabelField("bool");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("NoFrontLayer:");
        EditorGUILayout.LabelField(assertions.NoFrontLayer.ToString());
        EditorGUILayout.LabelField("bool");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Invisible:");
        EditorGUILayout.LabelField(assertions.NoBackLayer.ToString());
        EditorGUILayout.LabelField("bool");
        EditorGUILayout.EndHorizontal();
    }

    void EnvShake(EnvironmentShake shake)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("TimeElasped:");
        EditorGUILayout.LabelField(shake.TimeElasped.ToString());
        EditorGUILayout.LabelField("int");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Time:");
        EditorGUILayout.LabelField(shake.Time.ToString());
        EditorGUILayout.LabelField("int");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Frequency:");
        EditorGUILayout.LabelField(shake.Frequency.ToString());
        EditorGUILayout.LabelField("float");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Amplitude:");
        EditorGUILayout.LabelField(shake.Amplitude.ToString());
        EditorGUILayout.LabelField("float");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Phase:");
        EditorGUILayout.LabelField(shake.Phase.ToString());
        EditorGUILayout.LabelField("float");
        EditorGUILayout.EndHorizontal();
    }

    void Match(FightEngine engine)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Round.Time:");
        EditorGUILayout.LabelField(engine.TickCount.ToString());
        EditorGUILayout.LabelField("int");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("RoundNumber:");
        EditorGUILayout.LabelField(engine.RoundNumber.ToString());
        EditorGUILayout.LabelField("int");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("RoundState:");
        EditorGUILayout.LabelField((int)engine.RoundState + " - " + engine.RoundState.ToString());
        EditorGUILayout.LabelField("int");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("MatchNumber:");
        EditorGUILayout.LabelField(engine.MatchNumber.ToString());
        EditorGUILayout.LabelField("int");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("P1 Win:");
        EditorGUILayout.LabelField(engine.Team1.Wins.Count.ToString());
        EditorGUILayout.LabelField("int");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("P2 Win:");
        EditorGUILayout.LabelField(engine.Team1.Wins.Count.ToString());
        EditorGUILayout.LabelField("int");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("DrawGames:");
        EditorGUILayout.LabelField(engine.DrawGames.ToString());
        EditorGUILayout.LabelField("int");
        EditorGUILayout.EndHorizontal();
    }

    void Pause(Pause pause)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Hitpause:");
        EditorGUILayout.LabelField(pause.Hitpause.ToString());
        EditorGUILayout.LabelField("bool");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Pausebackgrounds:");
        EditorGUILayout.LabelField(pause.Pausebackgrounds.ToString());
        EditorGUILayout.LabelField("bool");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Time:");
        EditorGUILayout.LabelField((pause.Totaltime - pause.ElapsedTime).ToString());
        EditorGUILayout.LabelField("int");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Commandbuffertime:");
        EditorGUILayout.LabelField(pause.Commandbuffertime.ToString());
        EditorGUILayout.LabelField("int");
        EditorGUILayout.EndHorizontal();
    }


    void AnimationManager(AnimationManager animationManager)
    {
        if (!m_foldoutAnimations.ContainsKey(animationManager.GetHashCode()))
            m_foldoutAnimations.Add(animationManager.GetHashCode(), false);

        EditorGUILayout.BeginVertical("ObjectFieldThumb");
        {
            m_foldoutAnimations[animationManager.GetHashCode()] = EditorGUILayout.Foldout(m_foldoutAnimations[animationManager.GetHashCode()], "animation:", true, "Foldout");
            if (m_foldoutAnimations[animationManager.GetHashCode()])
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("action.no: ");
                    EditorGUILayout.LabelField(animationManager.CurrentAnimation.Number.ToString());
                    EditorGUILayout.LabelField("int");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("duration: ");
                    EditorGUILayout.LabelField(animationManager.CurrentAnimation.TotalTime.ToString());
                    EditorGUILayout.LabelField("int");
                }
                EditorGUILayout.EndHorizontal();

                if (animationManager.CurrentElement != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("elem.no: ");
                        EditorGUILayout.LabelField(animationManager.CurrentElement.Id.ToString());
                        EditorGUILayout.LabelField("int");
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Group/Index: ");
                        EditorGUILayout.LabelField(animationManager.CurrentElement.SpriteId.ToString());
                        EditorGUILayout.LabelField("sprite id");
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Axis: ");
                        EditorGUILayout.LabelField(animationManager.CurrentElement.Offset.ToString());
                        EditorGUILayout.LabelField("vector2");
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        EditorGUILayout.EndVertical();
    }

    void Cmd(ICommandManager commandManager)
    {
        if (!m_foldoutCmd.ContainsKey(commandManager.GetHashCode()))
            m_foldoutCmd.Add(commandManager.GetHashCode(), false);

        EditorGUILayout.BeginVertical("ObjectFieldThumb");
        {
            m_foldoutCmd[commandManager.GetHashCode()] = EditorGUILayout.Foldout(m_foldoutCmd[commandManager.GetHashCode()], "cmd.buffer.time:", true, "Foldout");
            if (m_foldoutCmd[commandManager.GetHashCode()])
            {
                foreach (UnityMugen.Commands.Command command in commandManager.Commands)
                {
                    int hasCodeCmd = commandManager.GetHashCode() + command.Name.GetHashCode();
                    int active = commandManager.CommandCount[command.Name].Value; /* .IsActive(command.Name) ? 1 : 0*/;

                    if (!m_toggleActiveCmd.ContainsKey(hasCodeCmd))
                        m_toggleActiveCmd.Add(hasCodeCmd, false);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUIUtility.labelWidth = 100;
                    m_toggleActiveCmd[hasCodeCmd] = EditorGUILayout.Toggle(command.Name, m_toggleActiveCmd[hasCodeCmd]);
                    if (active > 0 && m_toggleActiveCmd[hasCodeCmd] == true)
                        EditorApplication.isPaused = true;

                    EditorGUILayout.LabelField(active.ToString());
                    EditorGUILayout.LabelField("bool");
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        EditorGUILayout.EndVertical();
    }

    void ShowVars<T>(string label, ListIterator<T> variables, ref bool foldout)
    {
        EditorGUILayout.BeginVertical("ObjectFieldThumb");
        {
            foldout = EditorGUILayout.Foldout(foldout, label, true, "Foldout");
            if (foldout)
            {
                for (int i = 0; i < variables.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(string.Format("ID: {0}", i), m_optionsVars);
                    EditorGUILayout.LabelField(string.Format("VALUE: {0}", variables[i]));
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        EditorGUILayout.EndVertical();
    }
}