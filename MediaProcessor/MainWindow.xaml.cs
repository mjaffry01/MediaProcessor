// MainWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace MediaTimelineEditor
{
    public partial class MainWindow : Window
    {
        public List<MediaItem> MediaList { get; set; } = new List<MediaItem>();
        private MediaItem draggedItem = null;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        // Event for adding images
        private void AddImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.png;*.gif;*.bmp|All Files|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var imagePath in openFileDialog.FileNames)
                {
                    AddMediaToTimeline(imagePath, true);
                }
            }
        }

        // Event for adding videos
        private void AddVideo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Video Files|*.mp4;*.avi;*.mov;*.mkv|All Files|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var videoPath in openFileDialog.FileNames)
                {
                    AddMediaToTimeline(videoPath, false);
                }
            }
        }

        // Method to add media to the timeline
        private void AddMediaToTimeline(string filePath, bool isImage)
        {
            double defaultDuration = isImage ? 2.0 : GetVideoDuration(filePath);
            MediaItem mediaItem = new MediaItem
            {
                FilePath = filePath,
                IsImage = isImage,
                Duration = defaultDuration
            };
            MediaList.Add(mediaItem);
            TimelineItemsControl.ItemsSource = null;
            TimelineItemsControl.ItemsSource = MediaList;
        }

        // Event handler for removing media items
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null && button.DataContext is MediaItem mediaItem)
            {
                MediaList.Remove(mediaItem);
                TimelineItemsControl.ItemsSource = null;
                TimelineItemsControl.ItemsSource = MediaList;
            }
        }

        // Event handler for duration textbox losing focus
        private void DurationTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && textBox.DataContext is MediaItem mediaItem)
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
                }
                else
                {
                    MessageBox.Show("Please enter a valid duration.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    textBox.Text = mediaItem.Duration.ToString();
                }
            }
        }

        // Event handler for creating the final video
        private async void CreateVideo_Click(object sender, RoutedEventArgs e)
        {
            if (MediaList.Count == 0)
            {
                MessageBox.Show("Please add media to the timeline before creating a video.", "No Media", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string outputFile = OutputPathTextBox.Text;
            if (string.IsNullOrWhiteSpace(outputFile))
            {
                MessageBox.Show("Please specify an output file path.", "No Output Path", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate output directory
            string outputDir = Path.GetDirectoryName(outputFile);
            if (!Directory.Exists(outputDir))
            {
                MessageBox.Show("The specified output directory does not exist.", "Invalid Output Path", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get video settings (hardcoded for simplicity)
            string resolution = "1280x720"; // Default resolution
            string frameRate = "30";        // Default frame rate

            // Show progress
            ProcessingProgressBar.Visibility = Visibility.Visible;
            ProcessingProgressBar.IsIndeterminate = true;
            StatusTextBlock.Text = "Processing...";

            // Initialize MediaProcessor
            // Option 1: If FFmpeg is in PATH
            MediaProcessor mediaProcessor = new MediaProcessor();

            // Option 2: If FFmpeg is NOT in PATH, provide the absolute path
            // Uncomment the following line and provide the correct path if needed
            // MediaProcessor mediaProcessor = new MediaProcessor(@"C:\Path\To\ffmpeg.exe");

            var (success, errorMessage) = await mediaProcessor.CreateVideoFromMediaAsync(MediaList, outputFile, resolution, frameRate);

            // Hide progress
            ProcessingProgressBar.IsIndeterminate = false;
            ProcessingProgressBar.Visibility = Visibility.Collapsed;

            if (success)
            {
                StatusTextBlock.Text = "Video Created Successfully!";
                MessageBox.Show("Video created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusTextBlock.Text = "Error during video creation.";
                MessageBox.Show($"An error occurred while creating the video:\n{errorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Event handler for browsing output path
        private void BrowseOutputPath_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "MP4 Video|*.mp4",
                FileName = "final_video.mp4"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                OutputPathTextBox.Text = saveFileDialog.FileName;
            }
        }

        // Event handler for previewing the video
        private void PreviewVideo_Click(object sender, RoutedEventArgs e)
        {
            string outputFile = OutputPathTextBox.Text;
            if (File.Exists(outputFile))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(outputFile) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open video: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("The specified output file does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Event handler for saving a project
        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Project Files|*.json",
                FileName = "project.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(MediaList, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(saveFileDialog.FileName, json);
                    MessageBox.Show("Project saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Event handler for loading a project
        private void LoadProject_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Project Files|*.json",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(openFileDialog.FileName);
                    MediaList = JsonConvert.DeserializeObject<List<MediaItem>>(json);
                    TimelineItemsControl.ItemsSource = null;
                    TimelineItemsControl.ItemsSource = MediaList;
                    MessageBox.Show("Project loaded successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Drag-and-Drop Reordering Implementation
        private void TimelineItemsControl_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(MediaItem)) && draggedItem != null)
            {
                MediaItem droppedData = e.Data.GetData(typeof(MediaItem)) as MediaItem;
                ItemsControl itemsControl = sender as ItemsControl;
                Point position = e.GetPosition(itemsControl);
                int index = GetCurrentIndex(position);

                if (index < 0)
                {
                    index = MediaList.Count - 1;
                }

                if (index >= 0 && index < MediaList.Count)
                {
                    MediaList.Remove(droppedData);
                    MediaList.Insert(index, droppedData);
                    TimelineItemsControl.ItemsSource = null;
                    TimelineItemsControl.ItemsSource = MediaList;
                }

                draggedItem = null;
            }
        }

        private void TimelineItemsControl_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private int GetCurrentIndex(Point position)
        {
            int index = 0;
            foreach (var item in MediaList)
            {
                double itemWidth = 160; // Width + spacing
                if (position.X < (index + 1) * itemWidth)
                {
                    return index;
                }
                index++;
            }
            return MediaList.Count - 1;
        }

        // Implement dragging initiation on MediaItemControl
        private void MediaItemControl_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MediaItemControl control = sender as MediaItemControl;
            if (control != null && control.DataContext is MediaItem mediaItem)
            {
                draggedItem = mediaItem;
                DragDrop.DoDragDrop(control, mediaItem, DragDropEffects.Move);
            }
        }

        // Method to get video duration using FFprobe (requires FFmpeg to be installed and in PATH)
        private double GetVideoDuration(string videoPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $@"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 ""{videoPath}""",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    if (double.TryParse(output, out double duration))
                    {
                        return duration;
                    }
                }
            }
            catch
            {
                // Handle exceptions or log errors
            }
            return 5.0; // Default duration if unable to retrieve
        }
    }
}
