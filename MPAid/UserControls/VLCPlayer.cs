﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vlc.DotNet.Forms;
using System.Reflection;
using System.IO;
using MPAid.Models;
using MPAid.Cores;
using MPAid.Forms;
using MPAid.NewForms;
using System.Runtime.CompilerServices;

namespace MPAid.UserControls
{
    /// <summary>
    /// Class handling the VLC video player.
    /// </summary>
    public partial class VlcPlayer : UserControl
    {
        // All strings are kept here to make them eaiser to change.
        private string selectFolderString = "Select Vlc libraries folder.";
        private string invalidStateString = "Invalid State!";
        private string noVideoString = "No video recording was found for that sound.";
        private string invalidRecordingString = "Invalid recording!";

        // Used to keep track of the currently playing file.
        private string filePath;

        // Delegate required for VLC Player.
        delegate void delegatePlayer(Uri file, string[] pars);
        delegate void delegateStopper();

        // The list of recordings to play.
        private List<Word> wordsList;
        // The index of the current recording.
        private int currentRecordingIndex = 0;

        private int repeatTimes = 0;
        private int repeatsRemaining = 0;

        // Events used to send setting changes to the parent class.
        public event PropertyChangedEventHandler IndexChanged;
        public event PropertyChangedEventHandler RepeatChanged;

        /// <summary>
        /// Wrapper property for the list of recordings.
        /// </summary>
        public List<Word> WordsList
        {
            get
            {
                return wordsList;
            }

            set
            {
                wordsList = value;
            }
        }

        /// <summary>
        /// Wrapper property for the current index.
        /// </summary>
        public int CurrentRecordingIndex
        {
            get
            {
                return currentRecordingIndex;
            }
            set
            {
                currentRecordingIndex = value;
                NotifyIndexChanged();
            }
        }

        /// <summary>
        /// Wrapper property for the times to repeat a sound.
        /// </summary>
        public int RepeatTimes
        {
            get
            {
                return repeatTimes;
            }

            set
            {
                repeatsRemaining = value;
                repeatTimes = value;
                NotifyRepeatChanged();
            }
        }

        /// <summary>
        /// Called when the CurrentRecordingIndex property is changed, allowing the parent class to subscribe to the PropertyChanged event and update its own values.
        /// </summary>
        /// <param name="propertyName">Automatically filled in by the CallerMemberName annotation.</param>
        private void NotifyIndexChanged([CallerMemberName] String propertyName = "")
        {
            if (IndexChanged != null)
            {
                IndexChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Called when the RepeatTimes property is changed, allowing the parent class to subscribe to the PropertyChanged event and update its own values.
        /// </summary>
        /// <param name="propertyName">Automatically filled in by the CallerMemberName annotation.</param>
        private void NotifyRepeatChanged([CallerMemberName] String propertyName = "")
        {
            if (RepeatChanged != null)
            {
                RepeatChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Reference to the player control on the form.
        /// </summary>
        public VlcControl VlcControl
        {
            get { return vlcControl; }
        }


        /// <summary>
        /// Default constructor.
        /// </summary>
        public VlcPlayer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Fires when the control needs to have it's library directory assigned to it.
        /// Navigates to where the directories should be, and sets them, or asks the user to find the directory if it's not there.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void OnVlcControlNeedLibDirectory(object sender, VlcLibDirectoryNeededEventArgs e)
        {
            var currentAssembly = Assembly.GetEntryAssembly();  // Get the currently running project
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;    // Get the directory of the currently running project
            if (currentDirectory == null)   // If there isn't one, return.
                return;
            if (AssemblyName.GetAssemblyName(currentAssembly.Location).ProcessorArchitecture == ProcessorArchitecture.X86)  // If the computer is running an x86 architecture, use the x86 folder.
                e.VlcLibDirectory = new DirectoryInfo(Path.Combine(currentDirectory, @"VlcLibs\x86\"));
            else        // otherwise use the x64 folder.
                e.VlcLibDirectory = new DirectoryInfo(Path.Combine(currentDirectory, @"VlcLibs\x64\"));

            if (!e.VlcLibDirectory.Exists)      // If a folder is missing
            {
                var folderBrowserDialog = new FolderBrowserDialog();       // Raise a browser window and let the user find it.
                folderBrowserDialog.Description = selectFolderString;
                folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;
                folderBrowserDialog.ShowNewFolderButton = true;
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    e.VlcLibDirectory = new DirectoryInfo(folderBrowserDialog.SelectedPath);
                }
            }
        }

        /// <summary>
        /// Handles the functionality of the play/pause button, which differs based on the state of the player.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void playButton_Click(object sender, EventArgs e)
        {
            try
            {
                switch (vlcControl.State)
                {
                    case Vlc.DotNet.Core.Interops.Signatures.MediaStates.NothingSpecial:    // Occurs when control is finished loading. Same functionality as stopped.
                    case Vlc.DotNet.Core.Interops.Signatures.MediaStates.Ended:             // Occurs when control has finished playing a video. Same funcionaility as stopped.
                    case Vlc.DotNet.Core.Interops.Signatures.MediaStates.Stopped:
                        {
                            playVideo();
                        }
                        break;
                    case Vlc.DotNet.Core.Interops.Signatures.MediaStates.Playing:   // If playing, pause and update the button.
                        {
                            vlcControl.Pause();
                            playButton.ImageIndex = 1;
                        }
                        break;
                    case Vlc.DotNet.Core.Interops.Signatures.MediaStates.Paused:    // If paused, play and update the button.
                        {
                            asyncPlay();
                            playButton.ImageIndex = 3;
                        }
                        break;
                    default:
                        MessageBox.Show(invalidStateString);
                        break;
                }
            }
            catch (Exception exp)
            {
                
                MessageBox.Show(exp.Message);
                Console.WriteLine(exp);
            }
        }

        /// <summary>
        /// Plays or pauses the video, depending on the VLC player's current state.
        /// </summary>
        private void playVideo()
        {
            using (MPAidModel DBModel = new MPAidModel())
            {
                Word wd = wordsList[currentRecordingIndex];
                Speaker spk = null;   // Get the speaker from user settings.
                Recording rd = DBModel.Recording.SingleOrDefault(x => x.WordId == wd.WordId
                //&& x.SpeakerId == spk.SpeakerId // Comment this line out to test until we have user settings up and running.
                );    // Get the recording that corresponds to the speaker and word above.

                if (rd != null) // If the recording exists
                {
                    SingleFile sf = rd.Video;
                    if (sf == null)
                    {
                        MessageBox.Show(noVideoString);
                        return;
                    }
                    filePath = Path.Combine(sf.Address, sf.Name);
                    asyncPlay();
                    playButton.ImageIndex = 3;
                }
                else
                {
                    MessageBox.Show(invalidRecordingString);
                }
            }
        }

        /// <summary>
        /// The VLCPlayer is not threadsafe, so it is much easier to invoke it with delegates.
        /// </summary>
        private void asyncPlay()
        {
            // Get a new instance of the delegate
            delegatePlayer VLCDelegate = new delegatePlayer(vlcControl.Play);
            // Call play asynchronously.
            VLCDelegate.BeginInvoke(new Uri(filePath), new string[] { }, null, null);
        }

        /// <summary>
        /// The VLCPlayer is not threadsafe, so it is much easier to invoke it with delegates.
        /// </summary>
        private void asyncStop()
        {
            // Get a new instance of the delegate
            delegateStopper VLCDelegate = new delegateStopper(vlcControl.Stop);
            // Call stop asynchronously.
            VLCDelegate.BeginInvoke(null, null);
        }

        /// <summary>
        /// When the video is stopped, make the pause button into the play button, by swapping the images.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void OnVlcControlStopped(object sender, Vlc.DotNet.Core.VlcMediaPlayerStoppedEventArgs e)
        {
            playButton.ImageIndex = 1;
        }

        /// <summary>
        /// When the mouse hovers over the play button, it highlights.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void playButton_MouseEnter(object sender, EventArgs e)
        {
            playButton.ImageIndex = vlcControl.State != Vlc.DotNet.Core.Interops.Signatures.MediaStates.Playing ? 0 : 2;
        }

        /// <summary>
        /// When the mouse leaves the play button, stop it from highlighting.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void playButton_MouseLeave(object sender, EventArgs e)
        {
            playButton.ImageIndex = vlcControl.State != Vlc.DotNet.Core.Interops.Signatures.MediaStates.Playing ? 1 : 3;
        }

        /// <summary>
        /// When the mouse hovers over the back button, it highlights.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void backButton_MouseEnter(object sender, EventArgs e)
        {
            backwardButton.ImageIndex = 0;
        }

        /// <summary>
        /// When the mouse leaves the back button, stop it from highlighting.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void backButton_MouseLeave(object sender, EventArgs e)
        {
            backwardButton.ImageIndex = 1;
        }

        /// <summary>
        /// When the mouse hovers over the forward button, it highlights.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void forwardButton_MouseEnter(object sender, EventArgs e)
        {
            forwardButton.ImageIndex = 0;
        }

        /// <summary>
        /// When the mouse leaves the forward button, stop it from highlighting.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void forwardButton_MouseLeave(object sender, EventArgs e)
        {
            forwardButton.ImageIndex = 1;
        }

        /// <summary>
        /// When the video finishes playing, revert the play button to it's original state, and repeat the video if the user has set it to repeat.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void vlcControl_EndReached(object sender, Vlc.DotNet.Core.VlcMediaPlayerEndReachedEventArgs e)
        {
            // The only way to loop playback is to have a delegate call play asynchronously. 
            if (repeatTimes == 11)
            {
                asyncPlay();
            }
            else if (repeatsRemaining > 0)
            {
                asyncPlay();
                repeatsRemaining -= 1;
            }
            else
            {
                repeatsRemaining = RepeatTimes;
                playButton.ImageIndex = 1;
                // AutoPlay here
            }
        }

        /// <summary>
        /// Move to the next sound when the next button is clicked.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void forwardButton_Click(object sender, EventArgs e)
        { 
            if (CurrentRecordingIndex < WordsList.Count - 1)
            {
                CurrentRecordingIndex += 1;
            }
            else    // Move back to beginning if the user reaches the end of the list.
            {
                CurrentRecordingIndex = 0;
            }

            // If the video is playing, automatically play the next one.
            if (vlcControl.State.Equals(Vlc.DotNet.Core.Interops.Signatures.MediaStates.Playing))
            {
                playVideo();
            }
            else
            {
                asyncStop();  // So the old image doesn't stay on screen.
            }
            
        }

        /// <summary>
        /// Move to the previous sound when the previous button is clicked.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void backButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (CurrentRecordingIndex > 0)
                {
                    CurrentRecordingIndex -= 1;
                }
                else    // Move to the end if the user reaches the beginning of the list.
                {
                    CurrentRecordingIndex = WordsList.Count - 1;
                }

                // If the video is playing, automatically play the next one.
                if (vlcControl.State.Equals(Vlc.DotNet.Core.Interops.Signatures.MediaStates.Playing))
                {
                    playVideo();
                }
                else
                {
                    asyncStop();  // So the old image doesn't stay on screen.
                }
            }
            catch (FileNotFoundException exp)
            {
                // If there was no video for that recording, undo the action.
                currentRecordingIndex += 1;
            }
        }
    }
}
