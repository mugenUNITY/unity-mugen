using UnityEngine;
using UnityMugen.Combat;
using UnityMugen.Evaluation;
using UnityMugen.IO;
using UnityMugen.Video;

namespace UnityMugen.StateMachine.Controllers
{
#warning olhar novamente, testar esta class usando ela na pratica
    [StateControllerName("GraphicUI")]
    public class GraphicUI : StateController
    {
        public Expression m_update;
        public Expression m_id;
        public string m_name;
        public Expression m_pos;
        private PositionTypeUI? m_posType;
        public Expression m_scale;
        //public Expression m_animCommon;
        public PrefixedExpression m_anim;
        public Expression m_sprPriority;
        public Layer? m_layer;
        public Expression m_fillAmount;

        /// <summary>
        /// fillOrigin 0 = Left, 1 = Right
        /// </summary>
        public Expression m_fillOrigin;

        public Expression m_removeTime;
        private Blending? m_trans;
        public Expression m_alpha;
        public Expression m_color;

        public GraphicUI(StateSystem statesystem, string label, TextSection textsection)
            : base(statesystem, label, textsection) { }

        public override void Load()
        {
            if (isLoaded == false)
            {
                base.Load();

                m_update = textSection.GetAttribute<Expression>("update", null);
                m_name = textSection.GetAttribute<string>("name", null);
                m_id = textSection.GetAttribute<Expression>("id", null);

                m_pos = textSection.GetAttribute<Expression>("pos", null);
                m_posType = textSection.GetAttribute("postype", UnityMugen.PositionTypeUI.Center);
                m_scale = textSection.GetAttribute<Expression>("scale", null);

                m_anim = textSection.GetAttribute<PrefixedExpression>("anim", null);

                m_sprPriority = textSection.GetAttribute<Expression>("sprpriority", null);
                m_layer = textSection.GetAttribute("layer", Layer.Front);
                m_fillAmount = textSection.GetAttribute<Expression>("fillamount", null);
                m_fillOrigin = textSection.GetAttribute<Expression>("fillorigin", null);
                m_removeTime = textSection.GetAttribute<Expression>("removetime", null);
                m_color = textSection.GetAttribute<Expression>("color", null);

                m_trans = textSection.GetAttribute<Blending?>("trans", Misc.ToBlending(BlendType.None));
                m_alpha = textSection.GetAttribute<Expression>("alpha", null);

            }
        }

        public override void Run(Character character)
        {
            Load();



            var update = EvaluationHelper.AsBoolean(character, m_update, false);
            long Id = EvaluationHelper.AsInt32(character, m_id, int.MinValue);

            GraphicUIData graphicUIData = new GraphicUIData();
            graphicUIData.id = Id;

            if (!update)
            {
                graphicUIData.pos = EvaluationHelper.AsVector2(character, m_pos, Vector2.zero);
                graphicUIData.postype = m_posType;
                graphicUIData.scale = EvaluationHelper.AsVector2(character, m_scale, Vector2.one);

                graphicUIData.commonAnimation = EvaluationHelper.IsCommon(m_anim, false);
                graphicUIData.anim = EvaluationHelper.AsInt32(character, m_anim, null);

                graphicUIData.sprpriority = EvaluationHelper.AsInt32(character, m_sprPriority, 0);
                graphicUIData.layer = m_layer;
                graphicUIData.fillAmount = EvaluationHelper.AsInt32(character, m_fillAmount, 0);
                graphicUIData.fillOrigin = EvaluationHelper.AsInt32(character, m_fillOrigin, 0);
                graphicUIData.removetime = EvaluationHelper.AsInt32(character, m_removeTime, -2);
                graphicUIData.color = EvaluationHelper.AsVector4(character, m_color, Vector4.one);

                Vector2 alpha = EvaluationHelper.AsVector2(character, m_alpha, Vector2.zero);

                if (m_trans.Value.BlendType == BlendType.AddAlpha && alpha == Vector2.zero)
                {
                    Debug.LogWarning("Parametro Alpha requerido.");
                }
                else
                {
                    if (m_trans.Value.BlendType == BlendType.AddAlpha)
                        graphicUIData.transparency = new Blending(m_trans.Value.BlendType, alpha.x, alpha.y);
                    else
                        graphicUIData.transparency = Misc.ToBlending(m_trans.Value.BlendType);
                }
            }
            else
            {
                graphicUIData.pos = EvaluationHelper.AsVector2(character, m_pos, null);
                graphicUIData.postype = m_posType;
                graphicUIData.scale = EvaluationHelper.AsVector2(character, m_scale, null);

                graphicUIData.commonAnimation = EvaluationHelper.IsCommon(m_anim, default(bool));
                graphicUIData.anim = EvaluationHelper.AsInt32(character, m_anim, null);

                graphicUIData.sprpriority = EvaluationHelper.AsInt32(character, m_sprPriority, null);
                graphicUIData.layer = m_layer;
                graphicUIData.fillAmount = EvaluationHelper.AsInt32(character, m_fillAmount, null);
                graphicUIData.fillOrigin = EvaluationHelper.AsInt32(character, m_fillOrigin, null);
                graphicUIData.removetime = EvaluationHelper.AsInt32(character, m_removeTime, null);
                graphicUIData.color = EvaluationHelper.AsVector4(character, m_color, null);

                Blending? Transparency = textSection.GetAttribute<Blending?>("trans", null);
                Vector2? alpha = EvaluationHelper.AsVector2(character, m_alpha, null);

                if (Transparency.HasValue && alpha.HasValue &&
                    m_trans.Value.BlendType != BlendType.AddAlpha &&
                    alpha != Vector2.zero)
                {
                    if (m_trans.Value.BlendType == BlendType.AddAlpha)
                        graphicUIData.transparency = new Blending(m_trans.Value.BlendType, alpha.Value.x, alpha.Value.y);
                    else
                        graphicUIData.transparency = Misc.ToBlending(m_trans.Value.BlendType);
                }
            }


            if (!update)
            {
                if (graphicUIData.layer != Layer.Full)
                {
                    string nameGraphicUI = m_name + Id;
                    character.InstanceGraphicUI(nameGraphicUI, graphicUIData);
                }
                else
                {
                    character.Engine.stageScreen.CanvasFull.Set(character, graphicUIData);
                }
            }
            else
            {
                if (graphicUIData.layer != Layer.Full)
                {
                    foreach (var graphicUI in character.GetGraphicUIs(Id))
                        graphicUI.Modify(graphicUIData);
                }
                else
                {
                    character.Engine.stageScreen.CanvasFull.Set(character, graphicUIData);
                }
            }

        }

        public override bool IsValid()
        {
            if (base.IsValid() == false)
                return false;

            return true;
        }
    }
}

namespace UnityMugen.Combat
{
    public class GraphicUIData
    {
        public long? id { get; set; }
        public Vector2? pos { get; set; }
        public PositionTypeUI? postype { get; set; }
        public Vector2? scale { get; set; }
        public bool? commonAnimation { get; set; }
        public int? anim { get; set; }
        public int? sprpriority { get; set; }
        public Layer? layer { get; set; }
        public float? fillAmount { get; set; }
        public int? fillOrigin { get; set; }
        public int? removetime { get; set; }
        public Blending? transparency { get; set; }
        public Vector2? alpha { get; set; }
        public Color? color { get; set; }

        public long UniqueID { get; set; }
    }
}