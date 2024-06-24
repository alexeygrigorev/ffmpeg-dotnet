
using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Reflection;

using System.Text;
using System.Text.RegularExpressions;

using System.Runtime.InteropServices;
using System.Security.Cryptography;


namespace ffmpeg_dotnet
{
    public partial class Form1 : Form
    {
        private Button selectButton;
        private TextBox volumeEntry;
        private TextBox cutEntry;
        private CheckBox testRunCheck;
        private Button processButton;
        private Button quitButton;
        private OpenFileDialog openFileDialog;
        private string filename;

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        public Form1()
        {
            AllocConsole();  // Call this method at the start of the Form constructor.
    
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Video Processor";
            this.Width = 400;
            this.Height = 350;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create and place labels and controls for a cleaner layout
            Label selectLabel = new Label { Text = "Select Video File:", AutoSize = true, Top = 20, Left = 10 };
            Controls.Add(selectLabel);

            selectButton = new Button { Text = "Browse...", Top = 45, Left = 10, Width = 80 };
            selectButton.Click += SelectButton_Click;
            Controls.Add(selectButton);

            Label volumeLabel = new Label { Text = "Increase Volume By (dB):", AutoSize = true, Top = 80, Left = 10 };
            Controls.Add(volumeLabel);

            volumeEntry = new TextBox { Top = 105, Left = 10, Width = 100 };
            Controls.Add(volumeEntry);

            Label cutLabel = new Label { Text = "Cut Start Duration (seconds):", AutoSize = true, Top = 140, Left = 10 };
            Controls.Add(cutLabel);

            cutEntry = new TextBox { Top = 165, Left = 10, Width = 100, Text = "0.9" }; // Default value
            Controls.Add(cutEntry);

            testRunCheck = new CheckBox { Text = "Test Run (First 60 seconds)", Top = 200, Left = 10 };
            Controls.Add(testRunCheck);

            // Button to initiate processing
            processButton = new Button { Text = "Process", Top = 245, Left = 10, Width = 100, Height = 30 }; // Adjusted size
            processButton.Click += ProcessButton_Click;
            Controls.Add(processButton);

            // Button to quit the application
            quitButton = new Button { Text = "Quit", Top = 245, Left = 120, Width = 100, Height = 30, ForeColor = System.Drawing.Color.Red }; // Adjusted size
            quitButton.Click += (sender, args) => { Close(); };
            Controls.Add(quitButton);

            openFileDialog = new OpenFileDialog
            {
                Filter = "Video files (*.mp4;*.mkv;*.avi)|*.mp4;*.mkv;*.avi",
                Title = "Open Video File"
            };
        }


        private async void SelectButton_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog.FileName;
                double requiredGain = await AnalyzeSound();
                volumeEntry.Text = requiredGain.ToString("F2");
            }
        }
    
        private string GetExecutablePath(string executable)
        {
            var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(exePath ?? "", executable);
        }

        private async Task<double> AnalyzeSound()
        {
            string ffmpegPath = GetExecutablePath("ffmpeg.exe");
            string arguments = $"-i \"{filename}\" -af \"volumedetect, atrim=start=5\" -vn -sn -dn -f null -";

            var processOutput = await ExecuteCommandAsync(ffmpegPath, arguments);

            double meanVolume = ExtractMeanVolume(processOutput);

            if (!double.IsNaN(meanVolume))
            {
                double targetVolume = -25.7;
                double requiredGain = targetVolume - meanVolume;
                return requiredGain;
            }
            else
            {
                return 0; // Handling the case where mean volume couldn't be determined
            }
        }

        private double ExtractMeanVolume(string ffmpegOutput)
        {
            var match = Regex.Match(ffmpegOutput, @"mean_volume: ([-\d.]+) dB");
            if (match.Success)
            {
                return double.Parse(match.Groups[1].Value);
            }
            else
            {
                MessageBox.Show("Could not determine mean volume.");
                return double.NaN; // Return NaN to indicate failure
            }
        }


        private async void ProcessButton_Click(object sender, EventArgs e)
        {
            string ffmpegPath = GetExecutablePath("ffmpeg.exe");
            var volume = volumeEntry.Text;
            var cutTime = cutEntry.Text;
            var testRun = testRunCheck.Checked;
            var outputName = $"{Path.GetFileNameWithoutExtension(filename)}-out.mp4";
            var outputPath = Path.Combine(Path.GetDirectoryName(filename), outputName);

            // Delete the target file if it already exists
            if (File.Exists(outputPath))
            {
                try
                {
                    File.Delete(outputPath);
                    Console.WriteLine("Existing file deleted successfully.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete existing file: {ex.Message}");
                    return; // Exit the method to avoid running ffmpeg when file deletion fails
                }
            }

            var arguments = $"-ss {cutTime} -i \"{filename}\" -filter:a \"volume={volume}dB\" -vcodec copy";

            if (testRun)
            {
                arguments += " -t 60";
            }

            arguments += $" \"{outputPath}\"";

            await ExecuteCommandAsync(ffmpegPath, arguments);
            Console.WriteLine("Done");

            MessageBox.Show("Processing completed!");
        }
    
        public static async Task<string> ExecuteCommandAsync(string executablePath, string arguments)
        {
            Console.WriteLine($"Executing command: {executablePath} {arguments}");

            var processInfo = new ProcessStartInfo(executablePath, arguments)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,  // Also capture standard output
                UseShellExecute = false,
                CreateNoWindow = true
            };

            StringBuilder output = new StringBuilder();
            using (var process = new Process { StartInfo = processInfo })
            {
                process.Start();

                // Asynchronously read the output and error streams
                var readOutputTask = ReadStreamAsync(process.StandardOutput, output);
                var readErrorTask = ReadStreamAsync(process.StandardError, output);

                // Wait for both read operations to complete
                await Task.WhenAll(readOutputTask, readErrorTask);

                process.WaitForExit();  // Ensure the process has fully exited
                Console.WriteLine("Task complete");  // Log that the task is complete
            }

            return output.ToString();
        }

        private static async Task ReadStreamAsync(StreamReader stream, StringBuilder output)
        {
            char[] buffer = new char[1024]; // Adjust buffer size as needed
            int numberOfCharsRead;
            while ((numberOfCharsRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                string currentChunk = new string(buffer, 0, numberOfCharsRead);
                // Replace carriage returns with newlines or handle as needed
                currentChunk = currentChunk.Replace("\r", "\n");
                Console.Write(currentChunk);
                output.Append(currentChunk);
            }
        }
    }

    
}
