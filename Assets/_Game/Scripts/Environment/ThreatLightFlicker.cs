using System.Collections;
using CyberWatch.Audio;
using CyberWatch.Effects;
using CyberWatch.Threat;
using UnityEngine;

namespace CyberWatch.Environment
{
    /// <summary>
    /// Lampu kantor bereaksi pada Threat Level (sesuai dokumen desain):
    /// - Aman:    semua lampu normal.
    /// - Waspada: lampu utama kantor sesekali berkedip redup.
    /// - Bahaya:  lampu utama berkedip lalu MATI. Hanya lampu meja dan layar PC yang menyala.
    /// - Kritis:  lampu meja juga berkedip lalu MATI. Tersisa cahaya dari layar.
    /// Threat turun -> lampu menyala kembali (juga dengan berkedip).
    ///
    /// Memakai objek yang ada di scene (jangan ubah prefab Office_Environment-nya).
    /// </summary>
    public class ThreatLightFlicker : MonoBehaviour
    {
        [Tooltip("Otomatis dicari di scene kalau kosong.")]
        [SerializeField] private ThreatLevelSystem threat;

        [Header("Lampu utama kantor (mati saat Bahaya)")]
        [SerializeField] private Light[] ceilingLights;
        [Tooltip("Panel lampu langit-langit yang menyala (CeilingLight_Panel_Working).")]
        [SerializeField] private Renderer ceilingPanel;
        [Tooltip("Material panel saat mati (M_CeilingLight_Off).")]
        [SerializeField] private Material ceilingPanelOffMaterial;

        [Header("Lampu meja (mati saat Kritis)")]
        [SerializeField] private Light deskLamp;

        [Header("Kedip")]
        [Tooltip("Jeda antar kedip ringan saat Waspada (lampu utama).")]
        [SerializeField] private Vector2 alertDipInterval = new Vector2(9f, 18f);
        [Tooltip("Jeda antar kedip ringan saat Bahaya (lampu meja).")]
        [SerializeField] private Vector2 dangerDipInterval = new Vector2(12f, 24f);
        [Tooltip("Seberapa redup saat berkedip (0 = gelap total, 1 = tidak berubah).")]
        [SerializeField, Range(0f, 1f)] private float dipLevel = 0.15f;
        [Tooltip("Kekuatan statik layar saat lampu padam (0 = tanpa).")]
        [SerializeField, Range(0f, 1f)] private float lightOffGlitch = 0.7f;

        private float[] ceilingBase;
        private float deskBase;
        private Material panelOnMaterial;
        private ThreatStage stage;
        private bool ceilingOn = true;
        private bool deskOn = true;
        private Coroutine ceilingRoutine;
        private Coroutine deskRoutine;
        private bool poweredOff; // lose sequence: lights stay off for good
        private float nextCeilingDip;
        private float nextDeskDip;

        private void Awake()
        {
            if (threat == null)
                threat = FindAnyObjectByType<ThreatLevelSystem>();

            ceilingBase = new float[ceilingLights != null ? ceilingLights.Length : 0];
            for (int i = 0; i < ceilingBase.Length; i++)
                ceilingBase[i] = ceilingLights[i] != null ? ceilingLights[i].intensity : 0f;

            if (deskLamp != null)
                deskBase = deskLamp.intensity;
            if (ceilingPanel != null)
                panelOnMaterial = ceilingPanel.sharedMaterial;
        }

        private void OnEnable()
        {
            if (threat == null)
            {
                Debug.LogWarning("[ThreatLightFlicker] ThreatLevelSystem tidak ditemukan.", this);
                enabled = false;
                return;
            }

            threat.StageChanged += OnStageChanged;
            stage = threat.Stage;
            ApplyStage(stage, instant: true);
            ScheduleCeilingDip();
            ScheduleDeskDip();
        }

        private void OnDisable()
        {
            if (threat != null)
                threat.StageChanged -= OnStageChanged;

            // Kembalikan ke kondisi normal supaya scene tidak tertinggal gelap.
            if (poweredOff) return;
            SetCeiling(1f);
            SetDesk(1f);
        }

        private void OnStageChanged(ThreatStage previous, ThreatStage current)
        {
            stage = current;
            ApplyStage(current, instant: false);
        }

        private void ApplyStage(ThreatStage current, bool instant)
        {
            if (poweredOff)
                return;
            bool wantCeiling = current <= ThreatStage.Alert;  // Aman atau Waspada
            bool wantDesk = current <= ThreatStage.Danger;    // sampai Bahaya

            if (wantCeiling != ceilingOn || instant)
            {
                ceilingOn = wantCeiling;
                if (instant) SetCeiling(wantCeiling ? 1f : 0f);
                else Restart(ref ceilingRoutine, Transition(true, wantCeiling));
            }

            if (wantDesk != deskOn || instant)
            {
                deskOn = wantDesk;
                if (instant) SetDesk(wantDesk ? 1f : 0f);
                else Restart(ref deskRoutine, Transition(false, wantDesk));
            }
        }

        private void Update()
        {
            float now = Time.time;

            if (!poweredOff && ceilingOn && stage == ThreatStage.Alert && ceilingRoutine == null && now >= nextCeilingDip)
            {
                Restart(ref ceilingRoutine, Dip(true));
                ScheduleCeilingDip();
            }

            if (!poweredOff && deskOn && stage == ThreatStage.Danger && deskRoutine == null && now >= nextDeskDip)
            {
                Restart(ref deskRoutine, Dip(false));
                ScheduleDeskDip();
            }
        }

        /// <summary>True while the ceiling light is (meant to be) on.</summary>
        public bool CeilingOn => ceilingOn;

        /// <summary>True while the desk lamp is (meant to be) on.</summary>
        public bool DeskOn => deskOn;

        /// <summary>Lose sequence: the ceiling light sputters and dies for good (no-op if already dark).</summary>
        public IEnumerator DieOutCeiling()
        {
            poweredOff = true;
            bool wasOn = ceilingOn;
            ceilingOn = false;
            if (ceilingRoutine != null) { StopCoroutine(ceilingRoutine); ceilingRoutine = null; }
            if (wasOn) yield return Sputter(true);
            else SetCeiling(0f);
        }

        /// <summary>Lose sequence: the desk lamp sputters and dies for good (no-op if already dark).</summary>
        public IEnumerator DieOutDesk()
        {
            poweredOff = true;
            bool wasOn = deskOn;
            deskOn = false;
            if (deskRoutine != null) { StopCoroutine(deskRoutine); deskRoutine = null; }
            if (wasOn) yield return Sputter(false);
            else SetDesk(0f);
        }

        private IEnumerator Sputter(bool isCeiling)
        {
            int flickers = Random.Range(3, 6);
            PlayFlickerSound(isCeiling, 1f);
            for (int i = 0; i < flickers; i++)
            {
                SetChannel(isCeiling, i % 2 == 0 ? dipLevel : 0.7f);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.16f));
                SetChannel(isCeiling, 0f);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
            }
            SetChannel(isCeiling, 0f);
        }

        private void ScheduleCeilingDip() =>
            nextCeilingDip = Time.time + Random.Range(alertDipInterval.x, alertDipInterval.y);

        private void ScheduleDeskDip() =>
            nextDeskDip = Time.time + Random.Range(dangerDipInterval.x, dangerDipInterval.y);

        private void Restart(ref Coroutine routine, IEnumerator next)
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(next);
        }

        // Berkedip beberapa kali, lalu berhenti di keadaan akhir (menyala atau mati).
        private IEnumerator Transition(bool isCeiling, bool on)
        {
            int flickers = Random.Range(3, 6);
            PlayFlickerSound(isCeiling, on ? 0.6f : 1f);
            for (int i = 0; i < flickers; i++)
            {
                SetChannel(isCeiling, Random.value < 0.5f ? dipLevel : 1f);
                yield return new WaitForSeconds(Random.Range(0.04f, 0.14f));
            }

            SetChannel(isCeiling, on ? 1f : 0f);
            if (!on) ScreenGlitch.Trigger(lightOffGlitch);
            if (isCeiling) ceilingRoutine = null; else deskRoutine = null;
        }

        // Sound of the light sputtering, coming from the lamp itself (3D).
        private void PlayFlickerSound(bool isCeiling, float volume)
        {
            AudioLibrary clips = GameAudio.Clips;
            Light source = isCeiling
                ? (ceilingLights != null && ceilingLights.Length > 0 ? ceilingLights[0] : null)
                : deskLamp;
            if (clips == null || source == null)
                return;
            GameAudio.PlayAt(clips.lightFlicker, source.transform.position, AudioChannel.Sfx, volume, Random.Range(0.9f, 1.1f));
        }

        // Kedip ringan: redup sebentar (kadang dua kali) lalu normal.
        private IEnumerator Dip(bool isCeiling)
        {
            PlayFlickerSound(isCeiling, 0.45f);
            int count = Random.value < 0.35f ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                SetChannel(isCeiling, dipLevel);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
                SetChannel(isCeiling, 1f);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.12f));
            }

            if (isCeiling) ceilingRoutine = null; else deskRoutine = null;
        }

        private void SetChannel(bool isCeiling, float multiplier)
        {
            if (isCeiling) SetCeiling(multiplier);
            else SetDesk(multiplier);
        }

        private void SetCeiling(float multiplier)
        {
            if (ceilingLights != null)
            {
                for (int i = 0; i < ceilingLights.Length; i++)
                {
                    if (ceilingLights[i] == null) continue;
                    ceilingLights[i].intensity = ceilingBase[i] * multiplier;
                    ceilingLights[i].enabled = multiplier > 0.01f;
                }
            }

            if (ceilingPanel != null && ceilingPanelOffMaterial != null && panelOnMaterial != null)
                ceilingPanel.sharedMaterial = multiplier > 0.5f ? panelOnMaterial : ceilingPanelOffMaterial;
        }

        private void SetDesk(float multiplier)
        {
            if (deskLamp == null) return;
            deskLamp.intensity = deskBase * multiplier;
            deskLamp.enabled = multiplier > 0.01f;
        }
    }
}
