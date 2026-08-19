using HordeForge.Core;
using HordeForge.Core.Data;
using UnityEngine;

namespace HordeForge.Unity.View
{
    /// <summary>
    /// Farbpalette und Bausteine der Weltdarstellung.
    ///
    /// Der Prototyp kommt ohne importierte Assets aus: alles besteht aus Unity-
    /// Primitiven mit eingefaerbtem Standard-Material. Das haelt das Projekt frei
    /// von Binaerdateien und laeuft ueberall gleich.
    /// </summary>
    public static class Visuals
    {
        public static readonly Color Ground = new Color(0.29f, 0.38f, 0.24f);
        public static readonly Color Selection = new Color(1f, 0.92f, 0.35f);
        public static readonly Color PlacementValid = new Color(0.4f, 0.9f, 0.45f, 0.6f);
        public static readonly Color PlacementInvalid = new Color(0.9f, 0.32f, 0.28f, 0.6f);
        public static readonly Color SpawnMarker = new Color(0.75f, 0.18f, 0.18f);

        public static Color ForBuilding(BuildingCategory category)
        {
            switch (category)
            {
                case BuildingCategory.Core:
                    return new Color(0.86f, 0.71f, 0.30f);
                case BuildingCategory.Gathering:
                    return new Color(0.55f, 0.44f, 0.26f);
                case BuildingCategory.Production:
                    return new Color(0.62f, 0.36f, 0.22f);
                case BuildingCategory.Defense:
                    return new Color(0.42f, 0.50f, 0.62f);
                case BuildingCategory.Fortification:
                    return new Color(0.55f, 0.55f, 0.57f);
                default:
                    return new Color(0.52f, 0.36f, 0.62f);
            }
        }

        public static Color ForZone(BuildingType allowedBuilding)
        {
            switch (allowedBuilding)
            {
                case BuildingType.LumberCamp:
                    return new Color(0.18f, 0.42f, 0.20f);
                case BuildingType.Quarry:
                    return new Color(0.52f, 0.52f, 0.54f);
                case BuildingType.OreMine:
                    return new Color(0.40f, 0.31f, 0.24f);
                case BuildingType.GrainFarm:
                    return new Color(0.76f, 0.66f, 0.28f);
                case BuildingType.CottonFarm:
                    return new Color(0.80f, 0.80f, 0.74f);
                case BuildingType.SheepPasture:
                    return new Color(0.60f, 0.68f, 0.42f);
                case BuildingType.FishingHut:
                    return new Color(0.20f, 0.40f, 0.62f);
                default:
                    return new Color(0.45f, 0.45f, 0.45f);
            }
        }

        public static Color ForUnit(UnitType type)
        {
            switch (type)
            {
                case UnitType.Archer:
                    return new Color(0.45f, 0.72f, 0.90f);
                case UnitType.Spearman:
                    return new Color(0.30f, 0.62f, 0.58f);
                default:
                    return new Color(0.24f, 0.38f, 0.72f);
            }
        }

        public static Color ForEnemy(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Goblin:
                    return new Color(0.42f, 0.66f, 0.28f);
                case EnemyType.Ork:
                    return new Color(0.28f, 0.45f, 0.22f);
                case EnemyType.OrkWarrior:
                    return new Color(0.58f, 0.24f, 0.18f);
                case EnemyType.ShadowRider:
                    return new Color(0.42f, 0.24f, 0.60f);
                default:
                    return new Color(0.55f, 0.34f, 0.24f);
            }
        }

        /// <summary>
        /// Blendet die Grundfarbe abhaengig vom Zustand nach Rot – so ist auf einen
        /// Blick zu sehen, was gerade Schaden nimmt, ohne dass Lebensbalken noetig sind.
        /// </summary>
        public static Color Damaged(Color baseColor, float healthFraction)
        {
            if (healthFraction >= 1f)
            {
                return baseColor;
            }

            if (healthFraction < 0f)
            {
                healthFraction = 0f;
            }

            return Color.Lerp(new Color(0.75f, 0.12f, 0.10f), baseColor, healthFraction);
        }

        public static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Diffuse");
            }

            Material material = new Material(shader);
            material.color = color;

            if (color.a < 1f)
            {
                MakeTransparent(material);
            }

            return material;
        }

        /// <summary>Schaltet das Standard-Material in den Transparenzmodus.</summary>
        private static void MakeTransparent(Material material)
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }

        /// <summary>
        /// Erzeugt ein Primitiv ohne Collider. Auswahl und Platzierung rechnen mit einer
        /// mathematischen Bodenebene, deshalb wird die Physik gar nicht erst gebraucht.
        /// </summary>
        public static GameObject CreatePrimitive(
            PrimitiveType type, Transform parent, string name,
            Vector3 position, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial(color);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return go;
        }

        public static void SetColor(GameObject go, Color color)
        {
            if (go == null)
            {
                return;
            }

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                renderer.sharedMaterial.color = color;
            }
        }

        /// <summary>Weltkoordinate aus einer Kartenposition. Y ist in der Karte die Tiefe.</summary>
        public static Vector3 ToWorld(Vec2 position, float height = 0f)
        {
            return new Vector3(position.X, height, position.Y);
        }

        public static Vec2 ToMap(Vector3 position)
        {
            return new Vec2(position.x, position.z);
        }
    }
}
