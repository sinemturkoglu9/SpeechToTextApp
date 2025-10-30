# SpeechToTextApp

Bu uygulama Windows Forms (.NET 8) kullanarak mikrofondan ses kaydı alır ve Whisper (ggml-small) modeliyle konuşmayı yazıya çevirir.  
Kayıt bittikten sonra metin doğrudan ekrana yazılır. 

## Nasıl çalıştırılır?

1. Visual Studio 2022 (veya üstü) ile aç.
2. `NuGet` paketlerinin yüklü olduğundan emin ol:
   - `NAudio`
   - `Whisper.net`
3. Whisper modelini indir:
   - `ggml-small.bin` (Whisper small)
4. Bu dosyayı şu klasöre koy:
   - `bin/Debug/net8.0-windows/ggml-small.bin`
5. Projeyi Debug modda F5 ile çalıştır.

## Notlar
- Kayıt bittikten sonra metin TextBox’ta gözükür.
- Uygulama tek butonla çalışır: "Kaydı Başlat".
