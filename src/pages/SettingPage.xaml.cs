using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public partial class SettingPage : Page
    {
        public SettingPage()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();
            DataContext = Translator.Setting;

            // Initialize navigation
            NavigationView.SelectionChanged += NavigationView_SelectionChanged;

            // Initialize API combo box
            translateAPIBox.ItemsSource = Translator.Setting?.Configs.Keys;
            translateAPIBox.SelectedIndex = 0;
            LoadAPISetting();

            // Initialize target language combo box
            targetLangBox.SelectionChanged += targetLangBox_SelectionChanged;
            targetLangBox.LostFocus += targetLangBox_LostFocus;

            // Initialize LiveCaptions button text
            UpdateLiveCaptionsButtonText();

            // Set initial button state
            UpdateLiveCaptionsButtonText();
        }

        private void NavigationView_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // Handle navigation item selection

        }

        private void UpdateLiveCaptionsButtonText()
        {
            if (Translator.Window != null)
            {
                bool isHidden = Translator.Window.Current.BoundingRectangle == Rect.Empty;
                ButtonText.Text = isHidden ? "Show" : "Hide";
            }
        }

        private void LiveCaptionsButton_click(object sender, RoutedEventArgs e)
        {
            if (Translator.Window == null)
                return;

            bool isHidden = Translator.Window.Current.BoundingRectangle == Rect.Empty;
            if (isHidden)
            {
                LiveCaptionsHandler.RestoreLiveCaptions(Translator.Window);
                ButtonText.Text = "Hide";
            }
            else
            {
                LiveCaptionsHandler.HideLiveCaptions(Translator.Window);
                ButtonText.Text = "Show";
            }
        }

        private void translateAPIBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAPISetting();
        }

        private void targetLangBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (targetLangBox.SelectedItem != null)
            {
                Translator.Setting.TargetLanguage = targetLangBox.SelectedItem.ToString();
            }
        }

        private void targetLangBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Translator.Setting.TargetLanguage = targetLangBox.Text;
        }

        private void TargetLangButton_MouseEnter(object sender, MouseEventArgs e)
        {
            TargetLangInfoFlyout.Show();
        }

        private void TargetLangButton_MouseLeave(object sender, MouseEventArgs e)
        {
            TargetLangInfoFlyout.Hide();
        }

        private void LiveCaptionsInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            LiveCaptionsInfoFlyout.Show();
        }

        private void LiveCaptionsInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            LiveCaptionsInfoFlyout.Hide();
        }

        private void FrequencyInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            FrequencyInfoFlyout.Show();
        }

        private void FrequencyInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            FrequencyInfoFlyout.Hide();
        }

        private void CaptionLogMax_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            while (Translator.Caption.LogCards.Count > Translator.Setting.MainWindow.CaptionLogMax)
                Translator.Caption.LogCards.Dequeue();
            Translator.Caption.OnPropertyChanged("DisplayLogCards");
        }

        public void LoadAPISetting()
        {
            // Update target language combo box
            string targetLang = Translator.Setting.TargetLanguage;
            var supportedLanguages = Translator.Setting.CurrentAPIConfig.SupportedLanguages;
            targetLangBox.ItemsSource = supportedLanguages.Keys;

            // Add custom target language to ComboBox
            if (!supportedLanguages.ContainsKey(targetLang))
            {
                supportedLanguages[targetLang] = targetLang;
            }
            targetLangBox.SelectedItem = targetLang;

            // Show all API settings
            ShowAllAPISettings();
        }

        private void ShowAllAPISettings()
        {
            if (OpenAIGrid != null) OpenAIGrid.Visibility = Visibility.Visible;
            if (OllamaGrid != null) OllamaGrid.Visibility = Visibility.Visible;
            if (DeepLGrid != null) DeepLGrid.Visibility = Visibility.Visible;
            if (OpenRouterGrid != null) OpenRouterGrid.Visibility = Visibility.Visible;
            if (NoSettingGrid != null) NoSettingGrid.Visibility = Visibility.Collapsed;
        }
    }
}