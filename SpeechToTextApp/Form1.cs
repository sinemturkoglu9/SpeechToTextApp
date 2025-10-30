using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using Whisper.net;

namespace SpeechToTextApp
{
    public partial class Form1 : Form
    {
        private WaveInEvent? micSource;
        private MemoryStream? pcmBuffer;   // mikrofon PCM verisi burada
        private bool isRecording = false;

        private WhisperFactory? factory;
        private WhisperProcessor? processor;

        public Form1()
        {
            InitializeComponent();

            txtResult.ReadOnly = true;
            txtResult.Multiline = true;
            txtResult.ScrollBars = ScrollBars.Vertical;
            txtResult.WordWrap = true;

            btnStart.Text = "Kaydı Başlat";
        }

        // BUTON
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!isRecording)
            {
                // başlat
                try
                {
                    StartRecording();
                }
                catch (Exception ex)
                {
                    Log("[HATA] Kayıt başlatılamadı: " + ex.Message);
                    SafeStopRecording();
                }
            }
            else
            {
                // durdurup yazıya çevir (async ama await ETMİYORUZ -> debugger güvende)
                Log("⏹ Kayıt durduruluyor, lütfen bekleyin...");
                _ = StopAndTranscribeAsync(); // fire-and-forget
            }
        }

        // --- KAYDI BAŞLAT ---
        private void StartRecording()
        {
            Log("🎙 Kayıt başladı...");
            isRecording = true;
            btnStart.Text = "Kaydı Durdur";

            pcmBuffer = new MemoryStream();

            micSource = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 16, 1) // 16kHz mono
            };

            micSource.DataAvailable += (s, a) =>
            {
                pcmBuffer?.Write(a.Buffer, 0, a.BytesRecorded);
            };

            micSource.RecordingStopped += (s, a) =>
            {
                micSource?.Dispose();
                micSource = null;
            };

            micSource.StartRecording();
        }

        // --- SESSİZCE DURDUR ---
        private void SafeStopRecording()
        {
            if (!isRecording) return;
            isRecording = false;

            try
            {
                if (micSource != null)
                {
                    micSource.StopRecording(); // RecordingStopped eventi dispose eder
                }
            }
            catch
            {
                // yokmuş gibi davran
            }

            btnStart.Text = "Kaydı Başlat";
        }

        // --- DURDUR + WHISPER ---
        private async Task StopAndTranscribeAsync()
        {
            // 1) Kaydı durdur
            SafeStopRecording();
            Log("✔ Ses kaydı tamamlandı.");

            // 2) Buffer'ı kopyala (orijinali hemen serbest bırakacağız)
            if (pcmBuffer == null || pcmBuffer.Length == 0)
            {
                Log("⚠ Ses verisi yok.");
                return;
            }

            byte[] rawPcm = pcmBuffer.ToArray();
            long rawLen = rawPcm.Length;

            // orijinali bırak
            pcmBuffer.Dispose();
            pcmBuffer = null;

            Log($"[Debug] Kaydedilen veri boyutu: {rawLen} byte");

            if (rawLen < 3200)
            {
                Log("📝 Konuşma algılanmadı (çok kısa / sessiz).");
                Log("✅ Tamamlandı.");
                return;
            }

            // 3) WAV'e çevir -> tamamen bağımsız bir MemoryStream üret
            MemoryStream wavForWhisper = BuildWavFromPcm(rawPcm, 16000, 16, 1);

            // 4) Whisper model (lazy load)
            if (factory == null)
            {
                try
                {
                    // ggml-small.bin .exe ile aynı klasörde OLMALI
                    factory = WhisperFactory.FromPath("ggml-small.bin");
                    processor = factory.CreateBuilder()
                                       .WithLanguage("tr")
                                       .Build();
                }
                catch (Exception ex)
                {
                    Log("[HATA] Model yüklenemedi: " + ex.Message);
                    wavForWhisper.Dispose();
                    Log("✅ Tamamlandı.");
                    return;
                }
            }

            // 5) Transcribe
            Log("🧠 Ses işleniyor...");

            try
            {
                // wavForWhisper'i BAŞA sar
                wavForWhisper.Seek(0, SeekOrigin.Begin);

                await foreach (var segment in processor!.ProcessAsync(wavForWhisper))
                {
                    if (!string.IsNullOrWhiteSpace(segment.Text))
                    {
                        Log(segment.Text.Trim());
                    }
                }
            }
            catch (ObjectDisposedException ode)
            {
                // işte o sinir bozucu hata buraya düşse bile artık app patlamayacak
                Log("[HATA:ObjectDisposed] " + ode.Message);
            }
            catch (Exception ex)
            {
                Log("[HATA] " + ex.Message);
            }
            finally
            {
                // şimdi kapatmamız güvenli
                wavForWhisper.Dispose();
            }

            Log("✅ Tamamlandı.");
        }

        // --- PCM -> WAV MemoryStream oluştur ---
        private MemoryStream BuildWavFromPcm(byte[] pcmData, int sampleRate, int bits, int channels)
        {
            var wavMs = new MemoryStream();
            var fmt = new WaveFormat(sampleRate, bits, channels);

            // dikkat: WaveFileWriter stream'i kapatmasın diye leaveOpen:true
            using (var writer = new WaveFileWriter(wavMs, fmt) { })
            {
                writer.Write(pcmData, 0, pcmData.Length);
            }

            // writer using'den çıkınca writer dispose olur AMA
            // leaveOpen default'ta false; biz override etmedik -> hmm.
            // güvenli olsun diye şöyle yapalım: yeniden kopyalayacağız ki hiç risk kalmasın.

            // yukarıdaki leaveOpen'ı kontrol edemiyoruz çünkü ctor overload'ında yok.
            // WaveFileWriter dispose olduğunda wavMs'i KAPATIYOR.
            // Bu yüzden yukarıdaki yaklaşım hala riskli olabilir.
            // O yüzden: ikinci adımda yeni bir stream yapacağız.

            byte[] wavBytes = wavMs.ToArray(); // wavMs kapansa bile elimizde data var
            var finalStream = new MemoryStream(wavBytes, writable: false); // sadece okunur
            return finalStream;
        }

        // thread-safe log
        private void Log(string msg)
        {
            if (txtResult.InvokeRequired)
            {
                txtResult.Invoke(new Action(() =>
                {
                    txtResult.AppendText(msg + Environment.NewLine);
                    txtResult.ScrollToCaret();
                }));
            }
            else
            {
                txtResult.AppendText(msg + Environment.NewLine);
                txtResult.ScrollToCaret();
            }
        }
    }
}

