# PanelPlay

PanelPlay is an offline Android app for practicing AV troubleshooting on a photo of the equipment. Create a drill, place switches on the photo, and share it as a playable JSON file. The app uses .NET MAUI, MVVM, and Android SQLite.

## Build and run

Install the .NET 10 SDK, the MAUI Android workload, JDK 17, and Android SDK platform 36.

```powershell
dotnet workload install maui-android
dotnet build PanelPlay/PanelPlay.csproj
adb install -r PanelPlay/bin/Debug/net10.0-android/app.panelplay.prototype-Signed.apk
```

If the Android SDK or JDK is installed in a custom location, pass `-p:AndroidSdkDirectory=<path>` and `-p:JavaSdkDirectory=<path>` to the build.

## Credits

PanelPlay uses [.NET MAUI (MIT)](https://github.com/dotnet/maui/blob/main/LICENSE.txt), its [third-party notices](https://github.com/dotnet/maui/blob/main/THIRD-PARTY-NOTICES.TXT), and Android's [SQLite engine (public domain)](https://www.sqlite.org/copyright.html).
