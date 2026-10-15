using System;
using UnityEngine;

namespace CyberWatch.Environment
{
    /// <summary>
    /// GLOBAL night lighting settings, shared by every scene that has a Lighting_Night prefab.
    /// Edit the asset (Assets/_Game/Data/Lighting/) — changes show up immediately, also in Play Mode.
    /// Make a copy to try another look (e.g. a brighter "readable" variant) and assign it on Lighting_Night.
    ///
    /// Principle: only what a light actually hits is visible; everything else falls to black.
    /// Ambient, fog and reflections light EVERY surface equally (walls and roofs do not block them),
    /// so keep them near black — raising them is what makes dark areas look grey/blue and "gamey".
    /// </summary>
    [CreateAssetMenu(fileName = "LightingProfile_New", menuName = "Cyber Watch/Lighting Profile", order = 1)]
    public class LightingProfile : ScriptableObject
    {
        [Header("Bulan — cahaya utama di luar")]
        public Color moonColor = new Color(0.62f, 0.72f, 1f);
        [Range(0f, 2f)] public float moonIntensity = 0.15f;
        [Tooltip("Tinggi bulan dari horizon (derajat).")]
        [Range(5f, 90f)] public float moonElevation = 35f;
        [Tooltip("Arah datang bulan (derajat). 20 = dari belakang kantor, menyinari muka gedung seberang.")]
        [Range(-180f, 180f)] public float moonDirection = 20f;
        [Tooltip("1 = bayangan penuh (cahaya bulan tidak bocor ke dalam ruangan).")]
        [Range(0f, 1f)] public float moonShadowStrength = 1f;

        [Header("Ambient — cahaya merata di SEMUA permukaan (jaga tetap gelap)")]
        public Color ambientSky = new Color(0.012f, 0.012f, 0.016f);
        public Color ambientEquator = new Color(0.008f, 0.008f, 0.009f);
        public Color ambientGround = new Color(0.004f, 0.004f, 0.004f);
        [Tooltip("Pengali cepat untuk ketiga warna ambient (1 = apa adanya).")]
        [Range(0f, 4f)] public float ambientMultiplier = 1f;

        [Header("Fog — objek jauh tenggelam ke gelap")]
        public bool fog = true;
        [Tooltip("Samakan dengan warna langit agar objek jauh memudar ke gelap, bukan ke abu-abu.")]
        public Color fogColor = new Color(0.008f, 0.009f, 0.012f);
        [Range(0f, 0.2f)] public float fogDensity = 0.035f;

        [Header("Langit & pantulan")]
        [Tooltip("Warna langit malam (latar Main Camera).")]
        public Color skyColor = new Color(0.006f, 0.007f, 0.011f);
        [Tooltip("Pantulan langit pada permukaan mengkilap. 0 = tidak ada (tidak ada langit terang di malam hari).")]
        [Range(0f, 1f)] public float reflectionIntensity;

        /// <summary>Raised when any value changes in the Inspector, so scenes update live.</summary>
        public static event Action<LightingProfile> Changed;

        private void OnValidate() => Changed?.Invoke(this);
    }
}
