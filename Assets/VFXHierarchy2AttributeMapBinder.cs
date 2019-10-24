using System.Collections.Generic;
using TREESharp;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Hierarchy 2 to Attribute Map Binder")]
    [VFXBinder("Point Cache/Hierarchy 2 to Attribute Map")]
    class VFXHierarchy2AttributeMapBinder : VFXBinderBase
    {
        [VFXPropertyBinding("System.UInt32"), SerializeField]
        protected ExposedProperty m_BoneCount = "BoneCount";

        [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
        protected ExposedProperty m_PositionMap = "PositionMap";

        [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
        protected ExposedProperty m_TargetPositionMap = "TargetPositionMap";

        [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
        protected ExposedProperty m_ScaleMap = "ScaleMap";
        
        [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
        protected ExposedProperty m_ColorsMap = "ColorsMap";

        public Transform HierarchyRoot = null;
        public uint MaximumDepth = 3;
        public string nameFilter;
        public bool doUpdateHierarchy;

        private Texture2D position;
        private Texture2D targetPosition;
        private Texture2D scale;
        private Texture2D colors;
        private List<Bone> bones;

        private struct Bone
        {
            public Transform source;
            //public float sourceScale;
            public Transform target;
            public MeshRenderer mr;
//            public float targetScale;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateHierarchy();
            if (HierarchyRoot == null || HierarchyRoot.gameObject.activeSelf == false)
            {
                HierarchyRoot = FindObjectOfType<TREE>().transform;
            }
        }

        void OnValidate()
        {
            UpdateHierarchy();
        }


        void UpdateHierarchy()
        {
            bones = ChildrenOf(HierarchyRoot, MaximumDepth);
            int count = bones.Count;
            Debug.Log("Found Bone Count: " + count);

            position = new Texture2D(count, 1, TextureFormat.RGBAHalf, false, true);
            targetPosition = new Texture2D(count, 1, TextureFormat.RGBAHalf, false, true);
            scale = new Texture2D(count, 1, TextureFormat.RGBAHalf, false, true);
            colors = new Texture2D(count, 1, TextureFormat.RGBAHalf, false, true);

            UpdateData();
        }

        List<Bone> ChildrenOf(Transform source, uint depth)
        {
            List<Bone> output = new List<Bone>();
            if (source == null) return output;
            foreach (Transform child in source)
            {
                if (nameFilter == "" || child.gameObject.name == nameFilter)
                {
                    MeshRenderer mr;
                    mr = child.GetComponent<MeshRenderer>();
                    if (mr==null)
                    {
                        mr = child.GetComponentInChildren<MeshRenderer>();
                    }

                    output.Add(new Bone()
                    {
                        source = source.transform,
                        target = child.transform,
                        mr = mr
                        //sourceScale = DefaultScale,
                        //targetScale = DefaultScale,
                    });
                }

                if (depth > 0)
                {
                    output.AddRange(ChildrenOf(child, depth-1));
                }
            }
            return output;
        }


        void UpdateData()
        {
            int count = bones.Count;
            if (position.width != count) return;

            List<Color> positionList = new List<Color>();
            List<Color> targetList = new List<Color>();
            List<Color> scaleList = new List<Color>();
            List<Color> colorsList = new List<Color>();

            for (int i = 0; i < count; i++)
            {
                Bone b = bones[i];
                positionList.Add(new Color(b.source.position.x, b.source.position.y, b.source.position.z, 1));
                targetList.Add(new Color(b.target.position.x, b.target.position.y, b.target.position.z, 1));
                scaleList.Add(new Color(b.source.lossyScale.x, b.source.lossyScale.y, b.source.lossyScale.z, 1));
                colorsList.Add(b.mr.material.color);
            }
            position.SetPixels(positionList.ToArray());
            targetPosition.SetPixels(targetList.ToArray());
            scale.SetPixels(scaleList.ToArray());
            colors.SetPixels(colorsList.ToArray());

            position.Apply();
            targetPosition.Apply();
            scale.Apply();
        }


        public override bool IsValid(VisualEffect component)
        {
            return HierarchyRoot != null
                && component.HasTexture(m_PositionMap)
                && component.HasTexture(m_TargetPositionMap)
                && component.HasTexture(m_ScaleMap)
                && component.HasTexture(m_ColorsMap)
                && component.HasUInt(m_BoneCount);
        }

        public override void UpdateBinding(VisualEffect component)
        {

            if (bones.Count == 0 || doUpdateHierarchy)
            {
                UpdateHierarchy();
                doUpdateHierarchy = false;
            }
            else
            {
                UpdateData();
            }

            component.SetUInt(m_BoneCount, (uint)bones.Count);
            component.SetTexture(m_PositionMap, position);
            component.SetTexture(m_TargetPositionMap, targetPosition);
            component.SetTexture(m_ScaleMap, scale);
            component.SetTexture(m_ColorsMap, colors);
        }

        public override string ToString()
        {
            return string.Format("Hierarchy: {0} -> {1}", HierarchyRoot == null ? "(null)" : HierarchyRoot.name, m_PositionMap);
        }
    }
}
