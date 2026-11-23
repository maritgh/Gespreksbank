using System.Collections.Generic;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>Lichte geometrische kapsels en accessoires die het hoofd volgen.</summary>
    public sealed class AvatarCosmetics : MonoBehaviour
    {
        readonly List<GameObject> m_Parts = new List<GameObject>();
        readonly List<Material> m_Materials = new List<Material>();
        readonly List<Mesh> m_Meshes = new List<Mesh>();
        Material m_Hair, m_Frame, m_Accent, m_Shirt, m_Skin, m_Gold, m_Pearl, m_Pink;
        Mesh m_RingMesh, m_ChainMesh;
        int m_LastHair = -1, m_Layer;
        AvatarAccessory m_LastAccessories = (AvatarAccessory)(-1);

        public void Apply(int hair, Color hairColor, AvatarAccessory accessories, int layer = 0)
        {
            EnsureMaterials();
            m_Hair.color = hairColor;
            m_Layer = layer < 0 ? 0 : layer;
            hair = Mathf.Clamp(hair, 0, PlayerCustomization.HairNames.Length - 1);
            accessories = PlayerCustomization.NormalizeAccessories(accessories);
            if (hair == m_LastHair && accessories == m_LastAccessories)
            {
                foreach (var part in m_Parts) part.layer = m_Layer;
                return;
            }
            foreach (var part in m_Parts) { part.SetActive(false); Destroy(part); }
            m_Parts.Clear();
            m_LastHair = hair; m_LastAccessories = accessories;
            if (hair > 0)
                Part("Haarkap", PrimitiveType.Sphere, new Vector3(0, .12f, -.025f), new Vector3(.265f, .14f, .255f), m_Hair);
            switch (hair)
            {
                case 1:
                    for (int i = -1; i <= 1; i++)
                        Part("Lok", PrimitiveType.Sphere, new Vector3(i * .065f, .12f, .075f), new Vector3(.10f, .10f, .12f), m_Hair);
                    break;
                case 2:
                    for (int i = 0; i < 12; i++)
                    {
                        float angle = i * Mathf.PI / 6;
                        Part("Krul", PrimitiveType.Sphere, new Vector3(Mathf.Cos(angle) * .105f, .12f, Mathf.Sin(angle) * .105f - .02f),
                            Vector3.one * .085f, m_Hair);
                    }
                    Part("Bovenste krullen", PrimitiveType.Sphere, new Vector3(0, .18f, -.02f), new Vector3(.17f, .09f, .17f), m_Hair);
                    break;
                case 3:
                    Part("Lang haar achter", PrimitiveType.Sphere, new Vector3(0, -.015f, -.105f), new Vector3(.27f, .32f, .13f), m_Hair);
                    foreach (float side in new[] { -.12f, .12f })
                        Part("Lange zijlok", PrimitiveType.Sphere, new Vector3(side, -.035f, 0), new Vector3(.075f, .32f, .13f), m_Hair);
                    break;
                case 4:
                    for (int i = -2; i <= 2; i++)
                        Part("Hanenkam", PrimitiveType.Sphere, new Vector3(0, .21f, i * .045f), new Vector3(.065f, .17f, .07f), m_Hair);
                    break;
            }
            if ((accessories & (AvatarAccessory.Glasses | AvatarAccessory.Sunglasses)) != 0)
            {
                foreach (float x in new[] { -.071f, .071f })
                {
                    foreach (float y in new[] { -.033f, .033f })
                        Part("Brilmontuur", PrimitiveType.Cube, new Vector3(x, y + .035f, .15f), new Vector3(.12f, .012f, .018f), m_Frame);
                    foreach (float edge in new[] { -.055f, .055f })
                        Part("Brilmontuur", PrimitiveType.Cube, new Vector3(x + edge, .035f, .15f), new Vector3(.012f, .065f, .018f), m_Frame);
                    if ((accessories & AvatarAccessory.Sunglasses) != 0)
                        Part("Zonneglas", PrimitiveType.Cube, new Vector3(x, .035f, .151f), new Vector3(.10f, .053f, .014f), m_Frame);
                }
                Part("Brug", PrimitiveType.Cube, new Vector3(0, .045f, .15f), new Vector3(.035f, .014f, .018f), m_Frame);
                foreach (float x in new[] { -.13f, .13f })
                    Part("Brilpoot", PrimitiveType.Cube, new Vector3(x, .045f, .065f), new Vector3(.012f, .012f, .18f), m_Frame);
            }
            if ((accessories & AvatarAccessory.Headphones) != 0)
            {
                Part("Hoofdband", PrimitiveType.Sphere, new Vector3(0, .135f, -.025f), new Vector3(.34f, .065f, .11f), m_Frame);
                foreach (float x in new[] { -.153f, .153f })
                {
                    Part("Koptelefoonbeugel", PrimitiveType.Cube, new Vector3(x, .07f, -.025f), new Vector3(.026f, .14f, .05f), m_Frame);
                    Part("Koptelefoon", PrimitiveType.Sphere, new Vector3(x, 0, -.025f), new Vector3(.07f, .14f, .12f), m_Frame);
                    Part("Oorkussen", PrimitiveType.Sphere, new Vector3(x * 1.09f, 0, -.025f), new Vector3(.035f, .09f, .08f), m_Accent);
                }
            }
            if ((accessories & AvatarAccessory.PartyHat) != 0)
            {
                Part("Feesthoed", PrimitiveType.Cylinder, new Vector3(0, .235f, -.02f), new Vector3(.17f, .075f, .17f), m_Accent);
                Part("Hoedrand", PrimitiveType.Cylinder, new Vector3(0, .164f, -.02f), new Vector3(.25f, .012f, .25f), m_Frame);
                Part("Pompon", PrimitiveType.Sphere, new Vector3(0, .34f, -.02f), Vector3.one * .06f, m_Hair);
            }
            if ((accessories & AvatarAccessory.GoldNecklace) != 0)
            {
                if (m_ChainMesh == null) m_ChainMesh = Tube("Gouden ketting", .15f, .14f, -.18f, .08f, .006f);
                MeshPart("Gouden ketting", m_ChainMesh, Vector3.zero, Vector3.one, m_Gold);
                Part("Gouden hanger", PrimitiveType.Sphere, new Vector3(0, -.28f, .15f), new Vector3(.03f, .045f, .012f), m_Gold);
            }
            if ((accessories & AvatarAccessory.PearlNecklace) != 0)
                for (int i = 0; i < 20; i++)
                {
                    float angle = i * Mathf.PI * 2 / 20;
                    Part("Parel", PrimitiveType.Sphere,
                        new Vector3(.17f * Mathf.Sin(angle), -.19f - .055f * (1 + Mathf.Cos(angle)), .15f * Mathf.Cos(angle)),
                        Vector3.one * .022f, m_Pearl);
                }
            if ((accessories & AvatarAccessory.HairBow) != 0)
            {
                foreach (float x in new[] { .095f, .145f })
                    Part("Striklus", PrimitiveType.Sphere, new Vector3(x, .17f, -.01f), new Vector3(.07f, .055f, .035f), m_Pink);
                Part("Strikknoop", PrimitiveType.Sphere, new Vector3(.12f, .17f, .005f), Vector3.one * .035f, m_Accent);
            }
            if ((accessories & AvatarAccessory.HairClip) != 0)
            {
                var clip = Part("Haarclip", PrimitiveType.Cube, new Vector3(-.128f, .075f, .035f), new Vector3(.025f, .08f, .022f), m_Gold);
                clip.transform.localRotation = Quaternion.Euler(0, 0, -25);
                for (int i = -1; i <= 1; i++)
                    Part("Haarclip parel", PrimitiveType.Sphere, new Vector3(-.128f + i * .009f, .075f + i * .021f, .052f), Vector3.one * .017f, m_Pearl);
            }
            if ((accessories & AvatarAccessory.Flower) != 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    float angle = i * Mathf.PI * 2 / 5;
                    Part("Bloemblaadje", PrimitiveType.Sphere, new Vector3(-.11f + .025f * Mathf.Cos(angle), .17f + .025f * Mathf.Sin(angle), .055f),
                        new Vector3(.035f, .035f, .018f), m_Pink);
                }
                Part("Bloemhart", PrimitiveType.Sphere, new Vector3(-.11f, .17f, .065f), Vector3.one * .026f, m_Gold);
            }
            if ((accessories & AvatarAccessory.Earrings) != 0)
            {
                if (m_RingMesh == null) m_RingMesh = Tube("Oorbelring", 1, 1, 0, 0, .13f, true);
                foreach (float x in new[] { -.145f, .145f })
                    MeshPart("Oorbel", m_RingMesh, new Vector3(x, -.065f, .02f), new Vector3(.022f, .03f, .015f), m_Gold);
            }
        }

        public void BuildPreviewBody(Color shirtColor, Color skinColor)
        {
            EnsureMaterials();
            if (m_Shirt != null) { m_Shirt.color = shirtColor; m_Skin.color = skinColor; return; }
            m_Shirt = Material("Kleding", shirtColor);
            m_Skin = Material("Huidskleur", skinColor);
            PreviewPart("Hoofd", new Vector3(0, .025f, 0), new Vector3(.245f, .29f, .24f), m_Skin);
            PreviewPart("Nek", new Vector3(0, -.14f, -.01f), new Vector3(.09f, .12f, .09f), m_Skin);
            PreviewPart("Schouders", new Vector3(0, -.31f, -.015f), new Vector3(.42f, .33f, .24f), m_Shirt);
            foreach (float x in new[] { -.05f, .05f })
                PreviewPart("Oog", new Vector3(x, .02f, .115f), new Vector3(.024f, .032f, .014f), m_Frame);
            PreviewPart("Neus", new Vector3(0, -.025f, .13f), new Vector3(.035f, .045f, .04f), m_Skin);
        }

        void MeshPart(string name, Mesh mesh, Vector3 position, Vector3 scale, Material material)
        {
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.layer = m_Layer;
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            m_Parts.Add(part);
        }

        // Eén buismesh houdt een ketting licht genoeg voor VR; oorbellen hergebruiken dezelfde ringmesh.
        Mesh Tube(string name, float radiusX, float radiusZ, float y, float drop, float thickness, bool vertical = false)
        {
            const int segments = 32, sides = 6;
            var vertices = new Vector3[segments * sides];
            var triangles = new int[segments * sides * 6];
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments;
                Vector3 point = vertical ? new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0)
                    : new Vector3(radiusX * Mathf.Sin(a), y - drop * .5f * (1 + Mathf.Cos(a)), radiusZ * Mathf.Cos(a));
                Vector3 tangent = vertical ? new Vector3(Mathf.Cos(a), -Mathf.Sin(a), 0)
                    : new Vector3(radiusX * Mathf.Cos(a), drop * .5f * Mathf.Sin(a), -radiusZ * Mathf.Sin(a));
                tangent.Normalize();
                Vector3 normal = Vector3.Cross(tangent, vertical ? Vector3.forward : Vector3.up).normalized;
                Vector3 binormal = Vector3.Cross(tangent, normal);
                for (int j = 0; j < sides; j++)
                {
                    float b = j * Mathf.PI * 2 / sides;
                    int index = i * sides + j, next = ((i + 1) % segments) * sides + j;
                    vertices[index] = point + thickness * (normal * Mathf.Cos(b) + binormal * Mathf.Sin(b));
                    int corner = i * sides + (j + 1) % sides, nextCorner = ((i + 1) % segments) * sides + (j + 1) % sides;
                    int offset = index * 6;
                    triangles[offset] = index; triangles[offset + 1] = corner; triangles[offset + 2] = next;
                    triangles[offset + 3] = corner; triangles[offset + 4] = nextCorner; triangles[offset + 5] = next;
                }
            }
            var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); m_Meshes.Add(mesh);
            return mesh;
        }

        void PreviewPart(string name, Vector3 position, Vector3 scale, Material material)
        {
            // Voorbeeldonderdelen blijven bestaan wanneer alleen het kapsel wisselt.
            var part = Part(name, PrimitiveType.Sphere, position, scale, material);
            m_Parts.Remove(part);
        }

        GameObject Part(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.layer = m_Layer;
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            part.GetComponent<Renderer>().sharedMaterial = material;
            m_Parts.Add(part);
            return part;
        }

        void EnsureMaterials()
        {
            if (m_Hair != null) return;
            m_Hair = Material("Haar", Color.black);
            m_Frame = Material("Montuur", new Color(.10f, .12f, .18f));
            m_Accent = Material("Accent", new Color(.64f, .41f, .86f));
            m_Gold = Material("Goud", new Color(.95f, .67f, .18f));
            m_Gold.SetFloat("_Metallic", .65f);
            m_Pearl = Material("Parels", new Color(.97f, .94f, .86f));
            m_Pink = Material("Roze", new Color(.92f, .36f, .51f));
        }
        Material Material(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            m_Materials.Add(material);
            return material;
        }
        void OnDestroy()
        {
            foreach (var material in m_Materials) if (material != null) Destroy(material);
            foreach (var mesh in m_Meshes) if (mesh != null) Destroy(mesh);
        }
    }
}
