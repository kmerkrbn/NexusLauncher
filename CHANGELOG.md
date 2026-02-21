# Changelog

All notable changes to the Minecraft Launcher - Nexus project will be documented in this file.

## [1.0.3] - 2024 (Navigation & Profile Enhancement)

### Added
- ✅ **Enhanced MainPage Navigation**
  - Reorganized menu structure with Game Management section
  - Added About page
  - Future features placeholder (Mods, Worlds - Coming Soon)
  - Better visual hierarchy with separators

- ✅ **ProfilePage Complete Redesign**
  - New responsive layout with left info panel and right 3D viewer
  - Account information card with session status
  - Account statistics display
  - Skin settings panel (model selector, cape toggle)
  - Copy username to clipboard button
  - Better error handling

- ✅ **3D Skin Viewer Enhancements**
  - Walking animation (0.8 speed)
  - Auto-rotate feature (0.8 speed)
  - Mouse controls (drag to rotate)
  - Mouse wheel zoom support
  - Cape loading capability
  - Retry mechanism for failed loads
  - On-screen controls information
  - Gradient background
  - Interactive canvas with smooth controls

### Improved
- 🎨 **Navigation Experience** - Better organized menu with 3 sections
- 🎮 **Profile Display** - Full-featured player profile page
- ✨ **3D Viewer** - Now fully interactive and animated
- 📱 **Responsive Design** - Better layout for various screen sizes
- 🎯 **User Experience** - Clear instructions and controls

### Technical
- Services architecture enhanced
- Better WebView2 integration
- Improved error handling
- Clean code organization

## [1.0.2] - 2024 (Design Update)

### Added
- ✅ **HomePage Redesign** - Complete UI overhaul with modern dashboard
  - 4 Statistics Cards (Play Time, Versions, Last Played, Performance)
  - System Requirements Panel (RAM, Java, System Status)
  - Featured News Section with gradient background
  - Dynamic News Container with status badges
  - Emoji icons for better visual feedback
  - ScrollViewer for responsive scrolling

- ✅ **New UI Features**
  - Real-time statistics display
  - Java version detection
  - System status indicators
  - Gradient overlays and effects
  - Improved spacing and layout
  - Progress bars for system info

- ✅ **Code Enhancements**
  - UpdateStatistics() method
  - AddNewsItems() method
  - CreateNewsItem() dynamic UI generation
  - Better responsive design

### Improved
- 🎨 **Visual Design** - Modern, professional appearance
- 📊 **Information Density** - More data, better organized
- 🎯 **User Experience** - Easier navigation and information access
- 📱 **Responsive Layout** - Better spacing and proportions
- 🎬 **Visual Effects** - Gradient backgrounds and overlays

### Fixed
- 🐛 Empty space in featured section
- 🐛 Insufficient system information
- 🐛 Poor visual hierarchy

## [1.0.1] - 2024 (Bug Fixes)

### Fixed
- 🐛 **Premium Detection** - Fixed incorrect premium account detection for offline sessions
  - Changed from AccessToken length check to UUID format check for username
  - Microsoft accounts have UUID format username (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
  - Offline sessions no longer incorrectly show as Premium
  - More reliable premium/offline distinction

## [1.0.0] - 2024

### Added
- ✅ **SessionService** - New service-based session management replacing static getters
- ✅ **ISessionService Interface** - Abstraction for session management
- ✅ **ILogService & LogService** - Event-based logging service (ready for integration)
- ✅ **ISkinViewerService** - Separated skin viewer HTML generation from UI code
- ✅ **Version Refresh Button** - Manual version list refresh in Home page
- ✅ **Clear Logs Button** - Ability to clear console logs
- ✅ **Reset Settings Button** - One-click reset to default settings
- ✅ **CommonExtensions.cs** - String and integer utility extensions
- ✅ **Thread-Safe Logging** - Lock-based synchronization for ConsolePage
- ✅ **Global Exception Handler** - Application-wide exception handling in App.xaml.cs
- ✅ **README.md** - Comprehensive project documentation
- ✅ **CHANGELOG.md** - Version history and changes

### Improved
- 🔧 **Dependency Injection** - Pages now receive services through constructors
- 🔧 **Resource Management** - Proper HttpClient disposal in LoginPage
- 🔧 **Error Handling** - Better exception messages and user feedback
- 🔧 **Code Organization** - Services, Views, and Models clearly separated
- 🔧 **Settings Manager** - Added JSON property names and better error handling
- 🔧 **Premium Detection** - Consolidated premium account detection logic in SessionService
- 🔧 **HomePage** - Extracted launch logic into separate LaunchGame method
- 🔧 **ProfilePage** - Refactored to use ISkinViewerService
- 🔧 **Authentication** - Improved error handling in LoginPage with specific exception types
- 🔧 **RAM Validation** - Added min/max bounds checking (512MB - 32GB)
- 🔧 **Page Loaded Events** - Added proper Page_Loaded handlers for initialization

### Fixed
- 🐛 **MessageBox Ambiguity** - Resolved naming conflicts with WPF.UI and System.Windows
- 🐛 **OnUnloaded Override** - Changed to explicit cleanup method since Page doesn't support override
- 🐛 **Settings File Access** - Removed unnecessary File.ReadAllText() operation
- 🐛 **Navigation Pattern** - Replaced Window.GetWindow() pattern with service injection
- 🐛 **Null Reference Risks** - Added proper null checks in ProfilePage
- 🐛 **ConsolePage Singleton** - Added thread safety with lock mechanism
- 🐛 **WebView2 Error Handling** - Better error messages for WebView2 failures
- 🐛 **RAM Allocation** - Fixed numeric bounds validation

### Removed
- ❌ **Removed txtJavaArgs** - Not present in SettingsPage.xaml (code cleanup)
- ❌ **Removed static Session** - Replaced with ISessionService pattern
- ❌ **Removed Direct Window Access** - Pages now use service injection

### Security
- 🔒 Added thread-safe logging with lock synchronization
- 🔒 Proper session clearing on logout
- 🔒 Safe null handling throughout codebase
- 🔒 Improved error messages without exposing sensitive data

### Architecture
- 📐 **Service-Oriented Design** - All services implement interfaces
- 📐 **Dependency Injection Ready** - Pages accept services via constructors
- 📐 **Separation of Concerns** - UI logic separated from business logic
- 📐 **MVVM Foundation** - Structure ready for ViewModel layer addition

### Dependencies
- .NET 8 WPF
- WPF UI (Fluent Design)
- CMLLib (Minecraft Launcher Library)
- Microsoft.Web.WebView2
- System.Text.Json (built-in)

### Testing
- ✅ Full project builds successfully
- ✅ All pages load without errors
- ✅ Navigation between pages works correctly
- ✅ Settings persistence tested
- ✅ Exception handling verified

### Documentation
- 📖 README.md with architecture overview
- 📖 CHANGELOG.md (this file)
- 📖 Inline code comments for complex logic
- 📖 Interface documentation for services

### Notes for Future Development

#### Next Priority
1. Implement MVVM layer with proper ViewModels
2. Add unit tests for services
3. Integrate ILogService throughout application
4. Add theme customization

#### Technical Debt
- Consider moving to dependency injection container (e.g., Microsoft.Extensions.DependencyInjection)
- Refactor ConsolePage to use ILogService instead of static pattern
- Add proper error logging to file system
- Implement async/await patterns more consistently

#### Known Limitations
- 3D skin preview requires WebView2 runtime installation
- Game logs are in-memory only (cleared on app restart)
- Settings are local per user
- No cloud sync for settings

---

**Version**: 1.0.0  
**Release Date**: 2024  
**Status**: Production Ready ✅
