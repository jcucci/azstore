namespace AzStore.Terminal;

/// <summary>
/// Event arguments for navigation mode changes.
/// </summary>
public class NavigationModeChangedEventArgs : EventArgs
{
   /// <summary>
   /// Gets the previous navigation mode.
   /// </summary>
   public NavigationMode PreviousMode { get; }

   /// <summary>
   /// Gets the new navigation mode.
   /// </summary>
   public NavigationMode CurrentMode { get; }

   /// <summary>
   /// Initializes a new instance of the NavigationModeChangedEventArgs class.
   /// </summary>
   /// <param name="previousMode">The previous navigation mode.</param>
   /// <param name="currentMode">The new navigation mode.</param>
   public NavigationModeChangedEventArgs(NavigationMode previousMode, NavigationMode currentMode)
   {
      PreviousMode = previousMode;
      CurrentMode = currentMode;
   }
}