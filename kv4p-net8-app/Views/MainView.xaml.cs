using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using kv4p_net8_app.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace kv4p_net8_app.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            DataContext = App.Provider.GetService<MainViewModel>();
            Title = $"KV4P Net8 App v{Assembly.GetEntryAssembly().GetName().Version}";
            InitializeComponent();
        }

        private void OutputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            textBox?.ScrollToEnd();
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var box = sender as ComboBox;
            box.SelectedIndex = 0;
        }

        private void ComboBox_Loaded_1(object sender, RoutedEventArgs e)
        {
            var box = sender as ComboBox;
            box.SelectedIndex = 0;
        }
    }
}
