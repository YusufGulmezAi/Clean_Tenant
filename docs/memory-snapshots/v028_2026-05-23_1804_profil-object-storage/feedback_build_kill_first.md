---
name: Build öncesi çalışan kopyayı kapat
description: Her build/run öncesinde port 5081'de dinleyen ManagementApp process'i bulup kapat
type: feedback
originSessionId: 7e3ff30c-d6de-45ab-976e-8a70ec68b74b
---
Build etmeden önce çalışan ManagementApp kopyasını bul ve kapat, sonra derle ve çalıştır.

**Why:** dotnet build, çalışan process tarafından kilitlenen DLL'leri kopyalayamaz ve MSB3027 hatasıyla başarısız olur. Kullanıcı bunu her seferinde hatırlatmak zorunda kalmamalı.

**How to apply:** Her `dotnet build` veya `dotnet run` çağrısından önce:
1. `Get-NetTCPConnection -LocalPort 5081` ile dinleyen process'i bul
2. Bulunursa `Stop-Process -Force` ile kapat
3. Kısa bekleme (800ms) sonra derleme/çalıştırmaya geç
4. Çalıştırmak için her zaman `.\scripts\env-run.ps1 -Env Development -LaunchProfile http` kullan (connection string'ler .env.development dosyasından yüklenir)
