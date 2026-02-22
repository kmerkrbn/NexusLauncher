# Minecraft Launcher - Nexus
**A modern, secure Minecraft launcher built with .NET 8 WPF & Fluent Design**

## 🎮 Features

### Authentication
- ✅ **Microsoft OAuth Login** - Secure premium account authentication
- ✅ **Offline Mode** - Play offline with custom usernames
- ✅ **Session Management** - Persistent session handling with proper validation

### Game Launcher
- ✅ **Version Management** - Browse all Minecraft versions including snapshots
- ✅ **Auto Download** - Automatic file management and version installation
- ✅ **Real-time Progress** - Monitor download and launch progress with visual feedback
- ✅ **RAM Customization** - Adjust JVM heap size (512MB - 32GB)
- ✅ **Console Logging** - View game and launcher logs in real-time

### User Profile
- ✅ **3D Skin Viewer** - WebView2-based 3D skin preview with skinview3d library
- ✅ **Session Info** - Display account type (Premium/Offline)
- ✅ **Skin Refresh** - Reload skin preview on demand

### Settings
- ✅ **Snapshot Toggle** - Show/hide snapshot versions
- ✅ **RAM Allocation** - Configure JVM memory allocation
- ✅ **Default Reset** - Reset all settings to defaults with one click

<img width="802" height="99" alt="image" src="https://github.com/user-attachments/assets/739cf31a-f3a2-401c-a660-ecd1b6c509ca" />


## 🏗️ Architecture

### Design Patterns
- **Service Pattern** - Dependency injection through service interfaces
- **Repository Pattern** - Settings persistence with JSON serialization
- **Singleton Pattern** - Thread-safe console logging with lock-based synchronization
- **MVVM-ready** - UI separation with code-behind, prepared for ViewModel layer

### Project Structure
```
MClauncher/
├── Services/
│   ├── ISessionService.cs         # Session management interface
│   ├── SessionService.cs          # Session handling implementation
│   ├── ILogService.cs             # Logging interface
│   ├── ISkinViewerService.cs      # Skin viewer HTML generation
│   └── SkinViewerService.cs
├── Models/
│   └── SettingsManager.cs         # Settings persistence layer
├── Views/
│   ├── LoginPage.xaml(.cs)        # Authentication UI
│   ├── HomePage.xaml(.cs)         # Main launcher interface
│   ├── ProfilePage.xaml(.cs)      # User profile & skin viewer
│   ├── SettingsPage.xaml(.cs)     # Application settings
│   ├── ConsolePage.xaml(.cs)      # Game console output
│   └── MainPage.xaml(.cs)         # Navigation container
├── Extensions/
│   └── CommonExtensions.cs        # Utility extensions
├── MainWindow.xaml(.cs)           # Main application window
├── App.xaml(.cs)                  # Application entry point
└── MClauncher.csproj
```

## 🔧 Technical Stack

- **Framework**: .NET 8 WPF
- **UI Framework**: WPF UI (Fluent Design System)
- **Game Library**: CMLLib (Minecraft Launcher Library)
- **Web View**: WebView2 (for 3D skin preview)
- **Data**: JSON serialization with System.Text.Json
- **Threading**: Async/await patterns with Dispatcher for thread-safe UI updates

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK or later
- Windows 10/11 with WebView2 Runtime
- Visual Studio 2022 (recommended) or any .NET-compatible IDE

### Installation
1. Clone the repository
2. Open `MClauncher.csproj` in Visual Studio
3. Restore NuGet packages: `dotnet restore`
4. Build the solution: `dotnet build`
5. Run: `dotnet run` or press F5 in Visual Studio

### Configuration
Settings are automatically saved to:
```
%AppData%\.mclauncher\settings.json
```

## 📋 Recent Improvements

### Code Quality
- ✅ Replaced static getter patterns with service injection
- ✅ Implemented thread-safe logging with lock mechanism
- ✅ Fixed resource disposal (HttpClient, WebView2)
- ✅ Proper exception handling with user-friendly messages

### Architecture
- ✅ Separated concerns into Services, Views, and Models
- ✅ Created ISessionService for decoupled session management
- ✅ Created ISkinViewerService to extract HTML generation logic
- ✅ Implemented proper dependency injection in page constructors

### Features
- ✅ Added version refresh button in Home page
- ✅ Added clear logs button in Console page
- ✅ Added reset settings button in Settings page
- ✅ Improved error messages and user feedback
- ✅ Better RAM allocation validation (512MB - 32GB)

### UI/UX
- ✅ Enhanced Settings page with better organization
- ✅ Added visual feedback for all async operations
- ✅ Improved button layout and spacing
- ✅ Better error dialogs with consistent styling

## 🔐 Security Considerations

- **Microsoft OAuth**: Secure authentication via Microsoft's identity provider
- **Session Storage**: In-memory session management (no plaintext persistence)
- **HttpClient Management**: Proper resource disposal to prevent leaks
- **Input Validation**: Username validation for offline mode
- **Process Handling**: Sandboxed game process with output redirection

## 📚 Future Enhancements

### Planned Features
- [ ] MVVM ViewModel layer for full separation of concerns
- [ ] Theme customization (light/dark mode)
- [ ] Mod manager integration
- [ ] Custom Java arguments UI
- [ ] Server browser integration
- [ ] Auto-update mechanism
- [ ] Account switching

### Technical Debt
- [ ] Migrate to full MVVM with dependency injection container
- [ ] Add unit tests for services
- [ ] Implement proper logging service integration
- [ ] Add performance profiling for launcher startup

## 🐛 Known Issues & Fixes Applied

| Issue | Fix |
|-------|-----|
| HttpClient not disposed | Added proper disposal in LoginPage |
| Static ConsolePage reference | Added thread-safe locking mechanism |
| Settings file access | Removed unnecessary read operation |
| MessageBox ambiguity | Added explicit using aliases for System.Windows |
| Navigation to pages | Changed to use SessionService instead of GetSession() |

## 📝 License
This project is provided as-is for educational purposes.

## 👨‍💻 Contributing
Feel free to submit issues and enhancement requests!

---

**Last Updated**: 2024
**Version**: 1.0.0
**Status**: Production-Ready

