﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.IO;
using System.Diagnostics;
using MPAid.Models;
using MPAid.Cores;
using MPAid.Forms.MSGBox;

namespace MPAid.UserControls
{
    /// <summary>
    /// Class that handles the main recording and analysis tab of the main form.
    /// </summary>
    public partial class NAudioRecorder : UserControl
    {
        private IWaveIn waveIn;
        private WaveOutEvent waveOut = new WaveOutEvent();
        private WaveFileWriter writer;
        private WaveFileReader reader;
        private string outputFileName;
        private string outputFolder;
        private string tempFilename;
        private string tempFolder;
        private HTKEngine RecEngine = new HTKEngine();
        private ScoreBoard scoreBoard = new ScoreBoard();
        private NAudioPlayer audioPlayer = new NAudioPlayer();
        public NAudioPlayer AudioPlayer
        {
            get { return audioPlayer; }
        }
        /// <summary>
        /// Default constructor, also initialises the devices combo box.
        /// </summary>
        public NAudioRecorder()
        {
            InitializeComponent();

            LoadWasapiDevices();
        }
        /// <summary>
        /// Gets all the audio input devices attached to the system, converts them to a list, and populates the audio device combo box.
        /// </summary>
        public void LoadWasapiDevices()
        {
            var deviceEnum = new MMDeviceEnumerator();
            var devices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();

            audioDeviceComboBox.DataSource = devices.Count == 0 ? null : devices;
            audioDeviceComboBox.DisplayMember = "FriendlyName";
        }
        /// <summary>
        /// If the recordings folder doesn't already exist, creates it.
        /// Creates a temporary directory,to be used for recording purposes. 
        /// </summary>
        public void CreateDirectory()
        {
            outputFolder = Properties.Settings.Default.RecordingFolder;
            tempFolder = Path.Combine(Path.GetTempPath(), "MPAidTemp");
            Directory.CreateDirectory(outputFolder);
            Directory.CreateDirectory(tempFolder);
        }
        /// <summary>
        /// Clears the list box that holds all the recordings, and repopulates it with all valid recordings in the recordings directory.
        /// </summary>
        public void DataBinding()
        {
            RECListBox.Items.Clear();
            DirectoryInfo info = new DirectoryInfo(Properties.Settings.Default.RecordingFolder);
            RECListBox.Items.AddRange(info.GetFiles().Where(x => x.Extension != ".mfc").Select(x => x.Name).ToArray());
            // Deprecated: Old implemetation that gets all .wav files in the recording directory.
            //RECListBox.DataSource = info.GetFiles("*.wav");
        }
        /// <summary>
        /// Tidies up the stream after a wave file has been played or recorded.
        /// </summary>
        /// <param name="s">The stream to dispose of.</param>
        private void FinalizeWaveFile(Stream s)
        {
            if (s != null)
            {
                s.Dispose();
                s = null;
            }
        }
        /// <summary>
        /// Handles the state of the buttons based on whether or not the user is recording.
        /// </summary>
        /// <param name="isRecording"></param>
        private void SetControlStates(bool isRecording)
        {
            recordButton.Enabled = !isRecording;
            fromFileButton.Enabled = !isRecording;
            stopButton.Enabled = isRecording;
        }
        /// <summary>
        /// If a file is being recorded, stop recording and tidy up the stream.
        /// </summary>
        private void StopRecording()
        {
            if (waveIn != null) waveIn.StopRecording();
            FinalizeWaveFile(writer);
        }
        /// <summary>
        /// If a file is being played, stop playback and tidy up the stream.
        /// </summary>
        private void StopPlay()
        {
            if (waveOut != null) waveOut.Stop();
            FinalizeWaveFile(reader);
        }
        /// <summary>
        /// Event to handle audio buffering and updaating of the progress bar.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (InvokeRequired) // If it is necessary to invoke this method on a separate thread
            {
                // Optional: debug printout.
                //Debug.WriteLine("Data Available");
                BeginInvoke(new EventHandler<WaveInEventArgs>(OnDataAvailable), sender, e); // Send this event to the relevant thread.
            }
            else
            {
                // Optional: debug printout.
                //Debug.WriteLine("Flushing Data Available");
                writer.Write(e.Buffer, 0, e.BytesRecorded); // Record audio into a buffer
                int secondsRecorded = (int)(writer.Length / writer.WaveFormat.AverageBytesPerSecond);
                if (secondsRecorded >= 10)      // If we have recorded more than 10s of audio
                {
                    StopRecording();    // Stop recording
                }
                else
                {
                    recordingProgressBar.Value = secondsRecorded * 10;  // Otherwise, increase the progress bar.
                }
            }
        }
        /// <summary>
        /// Converts the temporary file created by recording into the format used by the recording files.
        /// </summary>
        private void Resample()
        {
            try
            {
                using (var reader = new WaveFileReader(Path.Combine(tempFolder, tempFilename))) // Read audio out of a temporary file in the temporary folder.
                {
                    var outFormat = new WaveFormat(16000, reader.WaveFormat.Channels);      // Define the output format of the audio
                    using (var resampler = new MediaFoundationResampler(reader, outFormat)) // Create the sampler that interprets the audio file into the format
                    {
                        // Optional: set sample quality.
                        //resampler.ResamplerQuality = 60;
                        WaveFileWriter.CreateWaveFile(Path.Combine(outputFolder, outputFileName), resampler);   // Use the resampler to create the .wav file in the recordings directory.
                    }
                }
                File.Delete(Path.Combine(tempFolder, tempFilename));    // Delete the temporary file.
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (InvokeRequired) // If it is necessary to invoke this on a different thread
            {
                BeginInvoke(new EventHandler<StoppedEventArgs>(OnRecordingStopped), sender, e); // Send this event to the relevant thread
            }
            else
            {
                Resample();
                recordingProgressBar.Value = 0; // Reset the progress bar
                if (e.Exception != null)
                {
                    MessageBox.Show(String.Format("A problem was encountered during recording {0}",
                                                  e.Exception.Message));
                }

                int newItemIndex = RECListBox.Items.Add(outputFileName);    // Add the new audio file to the list box
                RECListBox.SelectedIndex = newItemIndex;    // And select it
                SetControlStates(false);    // Toggle the record and stop buttons
            }
        }
        /// <summary>
        /// Handles the functionality behind the delete button. 
        /// The selected file is removed from the list box, and deleted from the user's computer.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void deleteButton_Click(object sender, EventArgs e)
        {
            if (RECListBox.SelectedItem != null)
            {
                try
                {
                    stopButton_Click(sender, e);
                    File.Delete(Path.Combine(outputFolder, (string)RECListBox.SelectedItem));
                    RECListBox.Items.Remove(RECListBox.SelectedItem);
                    if (RECListBox.Items.Count > 0)
                    {
                        RECListBox.SelectedIndex = 0;
                    }
                }
                catch (Exception exp)
                {
                    Console.WriteLine(exp);
                    MessageBox.Show("Could not delete recording");
                }
            }
        }
        /// <summary>
        /// Double clicking an item in the list of recordings will play that recording.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void RECListBox_DoubleClick(object sender, EventArgs e)
        {
            if (RECListBox.SelectedItem != null)
            {
                string filePath = Path.Combine(outputFolder, (string)RECListBox.SelectedItem);
                audioPlayer.Play(filePath);
            }
        }
        /// <summary>
        /// Handles the record button's functionality.
        /// Sets up the audio device, and the file to record into, adds listeners to the events, starts recording, and toggles the buttons.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void recordButton_Click(object sender, EventArgs e)
        {
            try
            {
                var device = (MMDevice)audioDeviceComboBox.SelectedItem;
                if (!device.Equals(null))
                {
                    device.AudioEndpointVolume.Mute = false;
                    // Use wasapi by default
                    waveIn = new WasapiCapture(device);
                    waveIn.DataAvailable += OnDataAvailable;
                    waveIn.RecordingStopped += OnRecordingStopped;

                    tempFilename = String.Format("{0}-{1:yyy-MM-dd-HH-mm-ss}.wav", MainForm.self.AllUsers.getCurrentUser().getLowerCaseName(), DateTime.Now);
                    // Initially, outputname is the same as tempfilename
                    outputFileName = tempFilename;
                    writer = new WaveFileWriter(Path.Combine(tempFolder, tempFilename), waveIn.WaveFormat);
                    waveIn.StartRecording();
                    SetControlStates(true);
                }
                else   
                {
                    MessageBox.Show("No audio device plugged in.",
                    "Oops", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            catch(Exception exp)
            {
#if DEBUG
                MessageBox.Show(exp.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
#endif
            }
        }
        /// <summary>
        /// Handles the Stop button's functionality.
        /// If the tool is recording or playing audio, stops. 
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void stopButton_Click(object sender, EventArgs e)
        {
            StopRecording();
            StopPlay();
        }
        /// <summary>
        /// Opens a file explorer so the user can add files to the recording folder. 
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void fromFileButton_Click(object sender, EventArgs e)
        {
            Process.Start(outputFolder);
        }
        /// <summary>
        /// Functionality for the analyse button.
        /// Sets the word to compare to and invokes the HTKEngine to identify what is being said in the recording.
        /// Also shows the results dialog box, and updates the score report.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void analyzeButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (RECListBox.SelectedItem != null)
                {
                    string target;
                    try
                    {
                        target = (MainForm.self.RecordingList.WordListBox.SelectedItem as Word).Name;
                    }
                    catch
                    {
                        target = string.Empty;
                    }
                    Dictionary<string, string> result = RecEngine.Recognize(Path.Combine(outputFolder, (string)RECListBox.SelectedItem)).ToDictionary(x => x.Key, x => x.Value);
                    if(result.Count > 0)
                    {
                        NAudioPlayer audioplayer = new NAudioPlayer();
                        audioplayer.Play(Path.Combine(outputFolder, (string)RECListBox.SelectedItem));
                        RecognitionResultMSGBox recMSGBox = new RecognitionResultMSGBox();
                        if (recMSGBox.ShowDialog(result.First().Key, target, result.First().Value) == DialogResult.OK)
                        {
                            scoreBoard.Content.Add(recMSGBox.scoreBoardItem);
                            correctnessLabel.Text = string.Format(@"Correctness: {0:0.0%}", scoreBoard.CalculateCorrectness); 
                        }
                        showReportButton.Enabled = true;
                    }
                }
                
            }
            catch (Exception exp)
            {
#if DEBUG
                MessageBox.Show(exp.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
#endif
            }
        }
        /// <summary>
        /// Functionality for the Show Report button
        /// Launches a web browser to view the score report.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void showReportButton_Click(object sender, EventArgs e)
        {
            ReportLaucher rl = new ReportLaucher();
            rl.GenerateHTML(scoreBoard);

            // Deprecated: this system no longer has an hConfig object.
            //String reportPath = hConfig.GetHtmlFullPath();

            // Show the HTML file in system browser
            if (File.Exists(rl.ReportAddr))
            {
                Process browser = new Process();
                browser.StartInfo.FileName = rl.ReportAddr;
                browser.Start();
            }
            else
                showReportButton.Enabled = false;
        }
        /// <summary>
        /// When the selected list box value changes, if the item is null, disables the analyse and delete buttons, and enables them if not.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void RECListBox_SelectedValueChanged(object sender, EventArgs e)
        {
            analyzeButton.Enabled = (sender as ListBox).SelectedItem != null;
            deleteButton.Enabled = (sender as ListBox).SelectedItem != null;
        }
        /// <summary>
        /// Functionality for the refresh devices button.
        /// Reloads any attached devices into the combo box, and refreshes it's value.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void deviceRefreshButton_Click(object sender, EventArgs e)
        {
            LoadWasapiDevices();
            DataBinding();
        }
        /// <summary>
        /// Raised when the control is redrawn. Take no particular action. 
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e){}
    }
}
