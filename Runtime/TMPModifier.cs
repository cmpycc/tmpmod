using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if ETWEEN_INCLUDED
using cmpy.Tween;
#endif

namespace cmpy.TMP
{
    public class TMPModifier : MonoBehaviour
    {
        private Dictionary<int, Character> characters;

        private TMP_Text text;
        private TMP_MeshInfo[] textMeshCache;

        private bool vertexUpdatePending = false;

        private void Awake()
        {
#if UNITY_EDITOR
            hideFlags = HideFlags.HideAndDontSave;
#endif
            text = GetComponent<TMP_Text>();
            text.ForceMeshUpdate(true);
            RefreshCache();

            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        }

        private void OnDestroy()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);

            if (characters != null)
            {
                foreach (KeyValuePair<int, Character> entry in characters)
                {
                    Destroy(entry.Value.transform);
                }
            }
        }

        private void Update()
        {
            if (!text)
            {
                Destroy(this);
                return;
            }
            if (!text.enabled) return;

            if (vertexUpdatePending)
            {
                UpdateVertexData();
                vertexUpdatePending = false;
            }
        }

        private void OnTextChanged(Object text)
        {
            if (text == this.text)
            {
                RefreshCache();
                UpdateVertexData();
            }
        }

        private void RefreshCache()
        {
            textMeshCache = text.textInfo.CopyMeshInfoVertexData();
        }

        public Character GetCharacter(int index)
        {
            if (characters == null) characters = new Dictionary<int, Character>();

            if (characters.TryGetValue(index, out Character character)) return character;
            else return CreateCharacter(index);
        }

        private Character CreateCharacter(int index)
        {
            Transform newTransform = new GameObject().transform;
            newTransform.SetParent(transform, false);

#if UNITY_EDITOR
            newTransform.gameObject.hideFlags = HideFlags.HideAndDontSave;
#endif

            Character character = new Character(this, newTransform);
            characters.Add(index, character);
            return character;
        }

        private void UpdateVertexData()
        {
            if (characters == null) return;

            foreach (KeyValuePair<int, Character> entry in characters)
            {
                if (entry.Key >= text.textInfo.characterCount) continue;

                TMP_CharacterInfo charInfo = text.textInfo.characterInfo[entry.Key];

                if (!charInfo.isVisible || entry.Value == null) continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;
                Vector3[] srcVertices = textMeshCache[materialIndex].vertices;

                Vector3 srcTL = srcVertices[vertexIndex + 1];
                Vector3 srcTR = srcVertices[vertexIndex + 2];
                Vector3 srcBL = srcVertices[vertexIndex];
                Vector3 srcBR = srcVertices[vertexIndex + 3];

                Vector3 offset = (srcTL + srcBR) * 0.5f;

                Vector3[] destVertices = text.textInfo.meshInfo[materialIndex].vertices;
                Matrix4x4 matrix = Matrix4x4.TRS(entry.Value.LocalPosition, entry.Value.LocalRotation, entry.Value.LocalScale);

                Vector3 destTL = matrix.MultiplyPoint3x4(srcTL - offset) + offset;
                Vector3 destTR = matrix.MultiplyPoint3x4(srcTR - offset) + offset;
                Vector3 destBL = matrix.MultiplyPoint3x4(srcBL - offset) + offset;
                Vector3 destBR = matrix.MultiplyPoint3x4(srcBR - offset) + offset;

                destVertices[vertexIndex + 1] = destTL;
                destVertices[vertexIndex + 2] = destTR;
                destVertices[vertexIndex] = destBL;
                destVertices[vertexIndex + 3] = destBR;

                if (entry.Value.colorModified)
                {
                    Color32[] destColors = text.textInfo.meshInfo[materialIndex].colors32;
                    destColors[vertexIndex + 1] = entry.Value.VertexGradient.topLeft;
                    destColors[vertexIndex + 2] = entry.Value.VertexGradient.topRight;
                    destColors[vertexIndex] = entry.Value.VertexGradient.bottomLeft;
                    destColors[vertexIndex + 3] = entry.Value.VertexGradient.bottomRight;
                }
            }

            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        public class Character
        {
            private readonly TMPModifier parent;
            internal readonly Transform transform; // using transforms for all the extra utility they carry.
            public bool colorModified { get; private set; }

            private VertexGradient _vertexGradient;

            #region Transform Properties
            public Vector3 Position
            {
                get => transform.position;
                set
                {
                    transform.position = value;
                    parent.vertexUpdatePending = true;
                }
            }

            public Vector3 LocalPosition
            {
                get => transform.localPosition;
                set
                {
                    transform.localPosition = value;
                    parent.vertexUpdatePending = true;
                }
            }

            public Quaternion Rotation
            {
                get => transform.rotation;
                set
                {
                    transform.rotation = value;
                    parent.vertexUpdatePending = true;
                }
            }

            public Quaternion LocalRotation
            {
                get => transform.localRotation;
                set
                {
                    transform.localRotation = value;
                    parent.vertexUpdatePending = true;
                }
            }

            public Vector3 EulerAngles
            {
                get => transform.eulerAngles;
                set
                {
                    transform.eulerAngles = value;
                    parent.vertexUpdatePending = true;
                }
            }

            public Vector3 LocalEulerAngles
            {
                get => transform.localEulerAngles;
                set
                {
                    transform.localEulerAngles = value;
                    parent.vertexUpdatePending = true;
                }
            }

            public float Angle
            {
                get => EulerAngles.z;
                set
                {
                    Vector3 euler = EulerAngles;
                    euler.z = value;
                    EulerAngles = euler;
                    parent.vertexUpdatePending = true;
                }
            }

            public float LocalAngle
            {
                get => LocalEulerAngles.z;
                set
                {
                    Vector3 euler = LocalEulerAngles;
                    euler.z = value;
                    LocalEulerAngles = euler;
                    parent.vertexUpdatePending = true;
                }
            }

            public Vector3 LocalScale
            {
                get => transform.localScale;
                set
                {
                    transform.localScale = value;
                    parent.vertexUpdatePending = true;
                }
            }
            #endregion

            #region Color Properties
            public VertexGradient VertexGradient
            {
                get => _vertexGradient;
                set
                {
                    _vertexGradient = value;
                    colorModified = true;
                    parent.vertexUpdatePending = true;
                }
            }

            public Color Color
            {
                get => VertexGradient.topLeft;
                set
                {
                    VertexGradient vertexGradient = VertexGradient;
                    vertexGradient.topLeft = vertexGradient.topRight = vertexGradient.bottomLeft = vertexGradient.bottomRight = (Color32)value;
                    VertexGradient = vertexGradient;
                }
            }

            public float Alpha
            {
                get => Color.a;
                set
                {
                    Color color = Color;
                    color.a = value;
                    Color = color;
                }
            }

            public void ClearColor()
            {
                colorModified = false;
            }
            #endregion

            internal Character(TMPModifier parent, Transform transform)
            {
                this.parent = parent;
                this.transform = transform;
                _vertexGradient = parent.text.colorGradient;
            }

#if ETWEEN_INCLUDED
            public Vector3Tween TPosition(Vector3 endValue, float duration)
            {
                Vector3Tween tween = new(
                    transform,
                    () => Position,
                    (value) => Position = value, endValue, duration);
                TweenEngine.Instance.AddTween(tween);
                return tween;
            }

            public Vector3Tween TLocalPosition(Vector3 endValue, float duration)
            {
                Vector3Tween tween = new(
                    transform,
                    () => LocalPosition,
                    (value) => LocalPosition = value, endValue, duration);
                TweenEngine.Instance.AddTween(tween);
                return tween;
            }

            public QuaternionTween TRotation(Quaternion endValue, float duration)
            {
                QuaternionTween tween = new(
                    transform,
                    () => Rotation,
                    (value) => Rotation = value, endValue, duration);
                TweenEngine.Instance.AddTween(tween);
                return tween;
            }

            public QuaternionTween TLocalRotation(Quaternion endValue, float duration)
            {
                QuaternionTween tween = new(
                    transform,
                    () => LocalRotation,
                    (value) => LocalRotation = value, endValue, duration);
                TweenEngine.Instance.AddTween(tween);
                return tween;
            }

            public Vector3Tween TLocalScale(Vector3 endValue, float duration)
            {
                Vector3Tween tween = new(
                    transform,
                    () => LocalScale,
                    (value) => LocalScale = value, endValue, duration);
                TweenEngine.Instance.AddTween(tween);
                return tween;
            }

            public ColorTween TColor(Color endValue, float duration)
            {
                ColorTween tween = new(
                    transform,
                    () => Color,
                    (value) => Color = value,
                    endValue, duration);
                TweenEngine.Instance.AddTween(tween);
                return tween;
            }

            public VertexGradientTween TGradient(VertexGradient endValue, float duration)
            {
                VertexGradientTween tween = new(
                    transform,
                    () => VertexGradient,
                    (value) => VertexGradient = value, endValue, duration);
                TweenEngine.Instance.AddTween(tween);
                return tween;
            }

            public FloatTween TAlpha(float endValue, float duration)
            {
                FloatTween tween = new(
                    transform,
                    () => Alpha,
                    (value) => Alpha = value,
                    endValue, duration);
                TweenEngine.Instance.AddTween(tween);
                return tween;
            }
#endif
        }
    }
}