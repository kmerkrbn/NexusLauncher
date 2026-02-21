# Minecraft Launcher - Development Guide

## Code Style & Standards

### Naming Conventions
```csharp
// Classes and Methods: PascalCase
public class SessionService { }
public void InitializeLauncher() { }

// Properties: PascalCase
public bool IsAuthenticated { get; }

// Private fields: camelCase with underscore prefix
private ISessionService _sessionService;
private MinecraftLauncher? _launcher;

// Local variables: camelCase
var versionName = cbVersions.SelectedItem.ToString();

// Constants: UPPER_CASE (if applicable)
private const string SettingsDirectory = ".mclauncher";
```

### Async/Await Patterns
```csharp
// Always use async/await
private async void Page_Loaded(object sender, RoutedEventArgs e)
{
    await InitializeLauncher();
}

private async System.Threading.Tasks.Task InitializeLauncher()
{
    // Async method implementation
}

// Use ConfigureAwait(false) in library code (not in UI code)
// In UI code (like this), ConfigureAwait is not necessary
```

### Null Handling
```csharp
// Use null-coalescing operator
var name = session?.Username ?? "Unknown";

// Use null-conditional operator
mainWindow?.Logout();

// Use null checks in critical sections
if (session == null)
{
    HandleAuthenticationError();
    return;
}
```

### Exception Handling
```csharp
// Catch specific exceptions
try 
{
    // Operation
}
catch (OperationCanceledException)
{
    lbStatus.Text = "Operation was cancelled.";
}
catch (Exception ex)
{
    lbStatus.Text = $"Error: {ex.Message}";
}

// Always include meaningful error messages
MessageBox.Show($"Failed to launch game:\n{ex.Message}", "Launch Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
```

### Service Pattern Implementation
```csharp
// Interface definition
public interface ISessionService
{
    MSession? GetSession();
    void SetSession(MSession session);
    bool IsAuthenticated { get; }
}

// Constructor injection
public ProfilePage(ISessionService sessionService)
{
    _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
}

// Use service instead of static getters
var session = _sessionService.GetSession();
```

## File Organization

### Class Structure Order
1. Using statements
2. Namespace declaration
3. Class declaration with XML comments
4. Constants and static fields
5. Properties
6. Constructors
7. Event handlers
8. Private methods
9. Nested classes (if any)

### Example Structure
```csharp
using System;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using MClauncher.Services;

namespace MClauncher.Views
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        private MinecraftLauncher? _launcher;
        private readonly ISessionService _sessionService;

        public HomePage(ISessionService sessionService)
        {
            InitializeComponent();
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        }

        private async void UiPage_Loaded(object sender, RoutedEventArgs e)
        {
            var session = _sessionService.GetSession();
            if (session == null)
            {
                HandleAuthenticationError();
                return;
            }

            await InitializeLauncher();
        }

        private async System.Threading.Tasks.Task InitializeLauncher()
        {
            try
            {
                // Implementation
            }
            catch (Exception ex)
            {
                lbStatus.Text = $"Error: {ex.Message}";
            }
        }

        private void HandleAuthenticationError()
        {
            MessageBox.Show("Your session has expired.", "Session Expired");
        }
    }
}
```

## Best Practices

### Threading in WPF
```csharp
// Always use Dispatcher for UI updates from other threads
ConsolePage.Log(message);  // Thread-safe with internal Dispatcher.Invoke

// Example implementation in service
Dispatcher.Invoke(() =>
{
    _instance.txtLogs.AppendText($"{message}{Environment.NewLine}");
});
```

### Resource Disposal
```csharp
// Implement proper cleanup
private readonly HttpClient _httpClient;

public LoginPage(ISessionService sessionService, MainWindow mainWindow)
{
    InitializeComponent();
    _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
    _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
    _httpClient = new HttpClient();
}

public void OnPageUnloaded()
{
    _httpClient?.Dispose();  // Explicit cleanup
}
```

### Settings Persistence
```csharp
// Load settings on initialization
static SettingsManager()
{
    Load();
}

// Save on every change
private void nbRam_SelectionChanged(object sender, RoutedEventArgs e)
{
    if (_isInitializing) return;
    
    SettingsManager.Current.RamAllocation = (int?)nbRam.Value ?? 2048;
    SettingsManager.Save();
}
```

### Validation
```csharp
// Always validate user input
private void btnLoginOffline_Click(object sender, RoutedEventArgs e)
{
    if (string.IsNullOrWhiteSpace(txtUsername.Text))
    {
        lbStatus.Text = "Please enter a username";
        return;
    }
    
    // Continue with login
}

// Validate numeric ranges
var value = (int?)nbRam.Value ?? 2048;
if (value < 512) value = 512;
if (value > 32768) value = 32768;
```

## XAML Guidelines

### Naming Controls
```xaml
<!-- Event handlers -->
<Button Name="btnLaunch" Content="Launch" Click="btnLaunch_Click" />
<Button Name="btnLogout" Content="Logout" Click="btnLogout_Click" />

<!-- Labels for display -->
<TextBlock Name="lbStatus" Text="Status..." />
<TextBlock Name="lbWelcome" Text="Welcome..." />

<!-- Input controls -->
<TextBox Name="txtUsername" PlaceholderText="Username" />
<ComboBox Name="cbVersions" Width="200" />

<!-- Toggle/Numeric controls -->
<ToggleSwitch Name="tsSnapshots" Content="Show Snapshots" />
<NumberBox Name="nbRam" Value="2048" />
```

### Layout Patterns
```xaml
<!-- Grid for complex layouts -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
        <RowDefinition Height="Auto" />
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="Auto" />
    </Grid.ColumnDefinitions>
</Grid>

<!-- StackPanel for linear layouts -->
<StackPanel Orientation="Vertical">
    <!-- Content -->
</StackPanel>

<!-- Cards for grouped content -->
<ui:Card Padding="20" Margin="0,0,0,10">
    <StackPanel>
        <!-- Content -->
    </StackPanel>
</ui:Card>
```

## Testing Checklist

### Before Committing
- [ ] Code builds successfully (`dotnet build`)
- [ ] No compiler warnings
- [ ] All pages load without errors
- [ ] Navigation between pages works
- [ ] Settings persist and load correctly
- [ ] Exception handling doesn't crash the app
- [ ] No null reference exceptions

### Functionality Testing
- [ ] Login with Microsoft works
- [ ] Offline login with username works
- [ ] Version list loads correctly
- [ ] Game launch initiates properly
- [ ] Profile page displays skin
- [ ] Settings can be changed and saved
- [ ] Console logs game output
- [ ] Logout returns to login page

## Performance Considerations

### Avoid
```csharp
// ❌ Don't block UI thread
File.ReadAllText(path);  // Blocking I/O
_launcher.GetAllVersionsAsync().Result;  // Blocking async

// ❌ Don't create unnecessary objects
for (int i = 0; i < 1000; i++)
{
    var obj = new HttpClient();  // Create once, reuse
}

// ❌ Don't ignore exceptions silently
try { } catch { }  // Always handle properly
```

### Prefer
```csharp
// ✅ Use async/await
await _launcher.GetAllVersionsAsync();

// ✅ Reuse objects
private readonly HttpClient _httpClient;

// ✅ Handle exceptions explicitly
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
}
```

## Common Patterns in This Project

### Page Initialization
```csharp
private async void Page_Loaded(object sender, RoutedEventArgs e)
{
    try
    {
        var session = _sessionService.GetSession();
        if (session == null)
        {
            HandleError();
            return;
        }
        
        UpdateUI(session);
        await LoadData();
    }
    catch (Exception ex)
    {
        lbStatus.Text = $"Error: {ex.Message}";
    }
}
```

### Event Handler Pattern
```csharp
private async void btnLaunch_Click(object sender, RoutedEventArgs e)
{
    btnLaunch.IsEnabled = false;  // Prevent double-click
    try
    {
        await PerformAction();
    }
    finally
    {
        btnLaunch.IsEnabled = true;
    }
}
```

### Logging Pattern
```csharp
ConsolePage.Log("=== Starting Launch Sequence ===");
ConsolePage.Log($"Version: {versionName}");
ConsolePage.Log($"RAM: {SettingsManager.Current.RamAllocation}MB");
```

## Future Improvements

### Recommended Refactoring
1. **Dependency Injection Container**
   ```csharp
   var services = new ServiceCollection();
   services.AddSingleton<ISessionService, SessionService>();
   services.AddSingleton<ILogService, LogService>();
   ```

2. **MVVM Implementation**
   ```csharp
   public class HomePageViewModel
   {
       private readonly ISessionService _sessionService;
       public ICommand LaunchGameCommand { get; }
   }
   ```

3. **Unit Testing**
   ```csharp
   [TestMethod]
   public void TestSessionService_SetSession()
   {
       var service = new SessionService();
       var session = MSession.CreateOfflineSession("TestUser");
       service.SetSession(session);
       Assert.IsTrue(service.IsAuthenticated);
   }
   ```

---

**Version**: 1.0.0  
**Last Updated**: 2024  
**Maintained By**: Development Team
