using System.Windows;
using DocuMind.Core.Models;

namespace DocuMind.UI.Views
{
    public partial class PersonaEditWindow : Window
    {
        public Persona? ResultPersona { get; private set; }

        public PersonaEditWindow()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text) || string.IsNullOrWhiteSpace(TxtSystemPrompt.Text))
            {
                MessageBox.Show("Lütfen isim ve talimat alanlarını doldurun.");
                return;
            }

            ResultPersona = new Persona
            {
                Name = TxtName.Text,
                Description = TxtDescription.Text,
                SystemPrompt = TxtSystemPrompt.Text,
                IconKind = (CmbIcon.SelectedItem as FrameworkElement)?.Tag?.ToString() ?? "Robot",
                IsDefault = false
            };

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
