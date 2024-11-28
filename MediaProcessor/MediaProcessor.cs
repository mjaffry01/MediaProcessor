// MediaProcessor.cs
using MediaTimelineEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MediaTimelineEditor
{
    public class MediaProcessor
    {
        private string ffmpegPath;

        public MediaProcessor(string ffmpegPath = "\"C:\\Users\\User\\source\\repos\\MediaConsoleApp\\MediaConsoleApp\\bin\\Debug\\net6.0\\ffmpeg.exe\"") // Ensure FFmpeg is in the system PATH
        {
            this.ffmpegPath = ffmpegPath;
        }

        /// <summary>
        /// Asynchronously combines photos and videos into one MP4 video.
        /// </summary>
        /// <param name="mediaItems">List of media items to process.</param>
        /// <param name="outputFile">Path to the output video file.</param>
        /// <param name="resolution">Desired resolution (e.g., "1280x720").</param>
        /// <param name="frameRate">Desired frame rate (e.g., "30").</param>
        /// <returns>Tuple indicating success status and error message.</returns>
        public async Task<(bool Success, string ErrorMessage)> CreateVideoFromMediaAsync(List<MediaItem> mediaItems, string outputFile, string resolution, string frameRate)
        {
            return await Task.Run<(bool Success, string ErrorMessage)>(() =>
            {
                try
                {
                    Console.WriteLine("Starting video creation process...");

                    // Parse resolution
                    string[] resParts = resolution.Split('x');
                    if (resParts.Length != 2 ||
                        !int.TryParse(resParts[0], out int targetWidth) ||
                        !int.TryParse(resParts[1], out int targetHeight))
                    {
                        Console.WriteLine("Invalid resolution format.");
                        return (false, "Invalid resolution format. Use WIDTHxHEIGHT, e.g., 1280x720.");
                    }

                    Console.WriteLine($"Target Resolution: {targetWidth}x{targetHeight}");
                    Console.WriteLine($"Frame Rate: {frameRate}");

                    // Use a fixed temporary directory for testing
                    string tempDir = Path.Combine("C:\\Temp\\MediaConsoleApp", "FixedFolderName");
                    Directory.CreateDirectory(tempDir);
                    Console.WriteLine($"Temporary directory created at: {tempDir}");

                    // Prepare media list for FFmpeg
                    string mediaListFile = Path.Combine(tempDir, "mediaList.txt");
                    using (StreamWriter writer = new StreamWriter(mediaListFile))
                    {
                        Console.WriteLine($"Creating media list file at: {mediaListFile}");

                        foreach (var media in mediaItems)
                        {
                            if (media.IsImage)
                            {
                                Console.WriteLine($"Processing image: {media.FilePath}");

                                // Construct the filter string
                                string filter = $@"scale=w={targetWidth}:h={targetHeight}:force_original_aspect_ratio=decrease," +
                                                $@"pad=w={targetWidth}:h={targetHeight}:x=(ow-iw)/2:y=(oh-ih)/2,format=yuv420p";

                                // Convert image to video segment with specified duration, preserving aspect ratio
                                string imageVideo = Path.Combine(tempDir, $"{Path.GetFileNameWithoutExtension(media.FilePath)}_{Guid.NewGuid()}.mp4");
                                string imageArgs = $@"-loop 1 -i ""{media.FilePath}"" " +
                                                   $@"-c:v libx264 -t {media.Duration} -pix_fmt yuv420p " +
                                                   $@"-vf ""{filter}"" " +
                                                   $@"-r {frameRate} -movflags +faststart ""{imageVideo}""";

                                // Log the FFmpeg command for debugging
                                Console.WriteLine("Executing FFmpeg command for image:");
                                Console.WriteLine($"{ffmpegPath} {imageArgs}");

                                var (successImage, errorImage) = ExecuteFFmpegCommand(imageArgs);
                                if (!successImage)
                                {
                                    Console.WriteLine($"Error processing image '{media.FilePath}': {errorImage}");
                                    return (false, $"Error processing image '{media.FilePath}': {errorImage}");
                                }

                                Console.WriteLine($"Image processed successfully: {imageVideo}");

                                writer.WriteLine($"file '{imageVideo}'");
                                writer.WriteLine($"duration {media.Duration}");
                            }
                            else
                            {
                                Console.WriteLine($"Adding video to media list: {media.FilePath}");
                                writer.WriteLine($"file '{media.FilePath}'");
                                // Optionally, you can trim or process videos here
                            }
                        }
                    }

                    Console.WriteLine("Media list file created successfully.");

                    // Combine all media segments into the final video
                    string arguments = $@"-f concat -safe 0 -i ""{mediaListFile}"" -c:v libx264 -pix_fmt yuv420p " +
                                       $@"-preset fast -r {frameRate} ""{outputFile}""";

                    // Log the FFmpeg command for concatenation
                    Console.WriteLine("Executing FFmpeg command for concatenation:");
                    Console.WriteLine($"{ffmpegPath} {arguments}");

                    var (successConcat, errorConcat) = ExecuteFFmpegCommand(arguments);
                    if (!successConcat)
                    {
                        Console.WriteLine($"Error during video concatenation: {errorConcat}");
                        return (false, $"Error during video concatenation: {errorConcat}");
                    }

                    Console.WriteLine("Video concatenation completed successfully.");

                    // Clean up temporary files
                    Directory.Delete(tempDir, true);
                    Console.WriteLine($"Temporary directory deleted: {tempDir}");

                    return (true, string.Empty);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception during video creation: {ex.Message}");
                    return (false, $"Exception during video creation: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Executes an FFmpeg command and captures its output.
        /// </summary>
        /// <param name="arguments">FFmpeg command arguments.</param>
        /// <returns>Tuple indicating success status and error message.</returns>
        private (bool Success, string ErrorMessage) ExecuteFFmpegCommand(string arguments)
        {
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = processStartInfo })
                {
                    // Event handlers for real-time output
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine(e.Data);
                        }
                    };

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Set a timeout (e.g., 2 minutes)
                    bool exited = process.WaitForExit(120000); // 120,000 ms = 2 minutes

                    if (!exited)
                    {
                        process.Kill();
                        Console.WriteLine("FFmpeg process timed out.");
                        return (false, "FFmpeg process timed out.");
                    }

                    // Wait for async output to complete
                    process.WaitForExit();

                    // Log the FFmpeg outputs
                    string logEntry = $"Command: {ffmpegPath} {arguments}\n" +
                                      $"Exit Code: {process.ExitCode}\n\n";
                    File.AppendAllText("ffmpeg_log.txt", logEntry);

                    if (process.ExitCode != 0)
                    {
                        // FFmpeg failed
                        return (false, "FFmpeg encountered an error. Check ffmpeg_log.txt for details.");
                    }

                    // FFmpeg succeeded
                    return (true, string.Empty);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions related to process start
                return (false, $"Exception while executing FFmpeg: {ex.Message}");
            }
        }
    }
}
