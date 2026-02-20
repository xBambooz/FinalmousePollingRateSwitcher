// Disambiguate WPF vs WinForms types (both are in scope because we use UseWPF + UseWindowsForms)
global using MessageBox = System.Windows.MessageBox;
global using MessageBoxButton = System.Windows.MessageBoxButton;
global using MessageBoxImage = System.Windows.MessageBoxImage;
global using MessageBoxResult = System.Windows.MessageBoxResult;
global using Application = System.Windows.Application;
global using KeyEventArgs = System.Windows.Input.KeyEventArgs;
global using Button = System.Windows.Controls.Button;
global using RadioButton = System.Windows.Controls.RadioButton;
global using FontFamily = System.Windows.Media.FontFamily;
global using Cursors = System.Windows.Input.Cursors;
