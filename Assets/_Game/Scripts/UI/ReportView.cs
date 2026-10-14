using System.Collections;
using System.Text;
using CyberWatch.Core;
using CyberWatch.Messages;
using CyberWatch.Session;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CyberWatch.UI
{
    /// <summary>
    /// Rapor Keamanan Cyber (GameOver_Report_Scene).
    /// Membaca GameManager.Instance.CurrentShift lalu mengisi judul, akurasi, skor,
    /// peringkat, dan daftar kesalahan beserta penjelasannya.
    /// Kalau scene ini di-Play langsung dari editor (belum ada data shift), data contoh dipakai.
    /// </summary>
    public class ReportView : MonoBehaviour
    {
        [Header("Teks")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text accuracyText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text gradeText;
        [SerializeField] private TMP_Text mistakesText;

        [Header("Tombol")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;
        [Tooltip("Aktifkan nanti setelah Office_TPP_Scene jadi. Sekarang Retry langsung ke Gameplay_Desktop.")]
        [SerializeField] private bool retryGoesToOffice;

        [Header("Efek")]
        [Tooltip("CanvasGroup di ReportRoot. Rapor muncul pelan dari layar hitam.")]
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField, Min(0f)] private float fadeInSeconds = 2f;

        [Header("Warna")]
        [SerializeField] private Color winColor = new Color(0.35f, 0.85f, 0.45f);
        [SerializeField] private Color loseColor = new Color(0.9f, 0.2f, 0.2f);

        [Header("Uji (hanya di Editor)")]
        [Tooltip("Kalau scene ini di-Play langsung dan belum ada data shift, pakai data contoh.")]
        [SerializeField] private bool useDemoDataWhenEmpty = true;
        [SerializeField] private ShiftOutcome demoOutcome = ShiftOutcome.Compromised;
        [Tooltip("Isi dengan beberapa file MSG_xx dari Assets/_Game/Data/Messages. Setiap pesan ke-3 sengaja dijawab salah.")]
        [SerializeField] private MessageData[] demoMessages;

        private void Awake()
        {
            // Jaga-jaga kalau scene sebelumnya mengunci kursor / mem-pause waktu.
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void Start()
        {
            ShiftSession shift = GameManager.Instance.CurrentShift;
            FillDemoDataIfNeeded(shift);
            Build(shift);

            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);

            if (fadeGroup != null)
                StartCoroutine(FadeIn());
        }

        private void OnDestroy()
        {
            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetry);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenu);
        }

        // ---------- isi rapor ----------

        private void Build(ShiftSession shift)
        {
            bool survived = shift.Outcome == ShiftOutcome.Survived;
            Color accent = survived ? winColor : loseColor;

            if (titleText != null)
            {
                titleText.text = survived ? "SHIFT SELESAI" : "SYSTEM COMPROMISED";
                titleText.color = accent;
            }

            if (subtitleText != null)
            {
                subtitleText.text = survived
                    ? "Anda bertahan sampai 06:00. Sistem kantor tetap aman."
                    : "Threat Level mencapai 100%. Kendali sistem diambil alih.";
            }

            int total = shift.Decisions.Count;
            int percent = Mathf.RoundToInt(shift.Accuracy * 100f);

            if (accuracyText != null)
                accuracyText.text = total == 0 ? "AKURASI  -" : $"AKURASI  {percent}%";

            if (scoreText != null)
                scoreText.text = $"BENAR  {shift.CorrectCount} / {total}";

            if (gradeText != null)
            {
                gradeText.text = total == 0 ? "PERINGKAT  -" : $"PERINGKAT  {GradeFor(percent)}";
                gradeText.color = accent;
            }

            if (mistakesText != null)
                mistakesText.text = BuildMistakes(shift);
        }

        private static string GradeFor(int percent)
        {
            if (percent >= 90) return "A  -  Penjaga Andal";
            if (percent >= 75) return "B  -  Waspada";
            if (percent >= 50) return "C  -  Perlu Latihan";
            return "D  -  Rentan Penipuan";
        }

        private static string BuildMistakes(ShiftSession shift)
        {
            if (shift.Decisions.Count == 0)
                return "Belum ada pesan yang diputuskan pada shift ini.";

            var mistakes = shift.GetMistakes();
            if (mistakes.Count == 0)
                return "Tidak ada kesalahan. Semua pesan berhasil Anda nilai dengan benar.";

            var sb = new StringBuilder();
            foreach (DecisionRecord m in mistakes)
            {
                MessageData msg = m.Message;
                sb.AppendLine($"<b>Pesan #{m.Number}</b>  [{ChannelLabel(msg.channel)}]  {msg.senderAddress}");
                sb.AppendLine($"Subjek: {msg.subject}");
                sb.AppendLine($"Pilihan Anda: {DecisionLabel(m.Decision)}   |   Seharusnya: {DecisionLabel(msg.CorrectDecision)}");

                if (msg.isScam && msg.indicators != ScamIndicator.None)
                    sb.AppendLine($"Tanda bahaya: {IndicatorsLabel(msg.indicators)}");

                if (!string.IsNullOrWhiteSpace(msg.explanation))
                    sb.AppendLine(msg.explanation);

                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static string ChannelLabel(MessageChannel c) => c == MessageChannel.Sms ? "SMS" : "Email";

        private static string DecisionLabel(PlayerDecision d) => d == PlayerDecision.Accept ? "TERIMA" : "BLOKIR";

        private static string IndicatorsLabel(ScamIndicator flags)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (flags.HasFlag(ScamIndicator.SuspiciousSender)) parts.Add("pengirim mencurigakan");
            if (flags.HasFlag(ScamIndicator.UrgentTone)) parts.Add("nada mendesak/mengancam");
            if (flags.HasFlag(ScamIndicator.AsksForCode)) parts.Add("meminta kode/OTP/password");
            if (flags.HasFlag(ScamIndicator.MaliciousLink)) parts.Add("tautan berbahaya");
            return string.Join(", ", parts);
        }

        // ---------- tombol ----------

        private void OnRetry()
        {
            if (retryGoesToOffice)
            {
                GameManager.Instance.StartNewShift();
            }
            else
            {
                GameManager.Instance.ResetShift();
                GameManager.Instance.StartDesktopPhase();
            }
        }

        private void OnMainMenu() => GameManager.Instance.GoToMainMenu();

        // ---------- efek ----------

        private IEnumerator FadeIn()
        {
            fadeGroup.alpha = 0f;
            float t = 0f;
            while (t < fadeInSeconds)
            {
                t += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Clamp01(t / fadeInSeconds);
                yield return null;
            }
            fadeGroup.alpha = 1f;
        }

        // ---------- data contoh (Editor saja) ----------

        private void FillDemoDataIfNeeded(ShiftSession shift)
        {
            if (!Application.isEditor || !useDemoDataWhenEmpty)
                return;
            if (shift.Outcome != ShiftOutcome.InProgress || shift.Decisions.Count > 0)
                return;
            if (demoMessages == null || demoMessages.Length == 0)
                return;

            for (int i = 0; i < demoMessages.Length; i++)
            {
                MessageData msg = demoMessages[i];
                if (msg == null) continue;

                bool answerWrong = i % 3 == 2;
                PlayerDecision decision = answerWrong
                    ? (msg.CorrectDecision == PlayerDecision.Accept ? PlayerDecision.Block : PlayerDecision.Accept)
                    : msg.CorrectDecision;

                shift.Record(msg, decision);
            }

            shift.Outcome = demoOutcome == ShiftOutcome.InProgress ? ShiftOutcome.Compromised : demoOutcome;
        }
    }
}