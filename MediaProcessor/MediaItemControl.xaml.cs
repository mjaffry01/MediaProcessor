// MediaItemControl.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;

namespace MediaTimelineEditor
{
    public partial class MediaItemControl : UserControl

    {
        public event EventHandler RemoveRequested;
        public event EventHandler DurationChanged;

        public MediaItemControl()
        {
           // InitializeComponent();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveRequested?.Invoke(this, EventArgs.Empty);
        }

        private void DurationTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && DataContext is MediaItem mediaItem)
            {
                if (double.TryParse(textBox.Text, out double newDuration))
                {
                    if (newDuration <= 0)
                    {
                        MessageBox.Show("Duration must be greater than zero.", "Invalid Duration", MessageBoxButton.OK, MessageBoxImage.Warning);
                        textBox.Text = mediaItem.Duration.ToString();
                        return;
                    }

                    mediaItem.Duration = newDuration;
                    DurationChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    MessageBox.Show("Please enter a valid duration.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    textBox.Text = mediaItem.Duration.ToString();
                }
            }
        }
    }
}
