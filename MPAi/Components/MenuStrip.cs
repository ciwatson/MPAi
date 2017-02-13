﻿using MPAi.Forms.Config;
using MPAi.Forms.MSGBox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MPAi.NewForms
{
    /// <summary>
    /// The menu strip that appears at the top of each form.
    /// </summary>
    public partial class MenuStrip : System.Windows.Forms.MenuStrip
    {
        private string changetext = "Your settings have been changed. You may need to reload the current form for your changes to take effect.";
        /// <summary>
        /// Default constructor.
        /// </summary>
        public MenuStrip() : base()
        {
            InitializeComponent();

            checkAndSetNativeDisplayVoice();
            checkAppropriateComponents();
            authoriseAdmin();
        }

        /// <summary>
        /// Constructor that adds this menu to a container.
        /// </summary>
        /// <param name="container">The container to add this menu to, as an IContainer.</param>
        public MenuStrip(IContainer container) : base()
        {
            container.Add(this);

            InitializeComponent();
            checkAndSetNativeDisplayVoice();
            checkAppropriateComponents();
            authoriseAdmin();
        }

        private void checkAndSetNativeDisplayVoice()
        {
            MPAi.Models.VoiceType? voiceType = UserManagement.CurrentUser.Voice;
            if (voiceType.Equals(MPAi.Models.VoiceType.FEMININE_MODERN) || voiceType.Equals(MPAi.Models.VoiceType.FEMININE_NATIVE))
            {
                nativeMāoriToolStripMenuItem.Text = MPAi.Models.DisplayVoice.DisplayNative(MPAi.Models.Gender.FEMININE);
            }
            else
            {
                nativeMāoriToolStripMenuItem.Text = MPAi.Models.DisplayVoice.DisplayNative(MPAi.Models.Gender.MASCULINE);
            }
        }

        /// <summary>
        /// Hides certain functions of the menu bar if the user is not an administrator.
        /// </summary>
        private void authoriseAdmin()
        {
            configToolStripMenuItem.Visible = UserManagement.currentUserIsAdmin();

            changePasswordToolStripMenuItem.Visible = !UserManagement.currentUserIsAdmin();
            consoleToolStripMenuItem.Visible = UserManagement.currentUserIsAdmin();
        }

        /// <summary>
        /// Checks the approrpiate checked controls based on the current user's voice settings.
        /// </summary>
        private void checkAppropriateComponents()
        {
            switch (UserManagement.CurrentUser.Voice)
            {
                case Models.VoiceType.FEMININE_NATIVE:
                    nativeMāoriToolStripMenuItem.Checked = true;
                    feminineToolStripMenuItem.Checked = true;
                    break;
                case Models.VoiceType.FEMININE_MODERN:
                    modernMāoriToolStripMenuItem.Checked = true;
                    feminineToolStripMenuItem.Checked = true;
                    break;
                case Models.VoiceType.MASCULINE_NATIVE:
                    nativeMāoriToolStripMenuItem.Checked = true;
                    masculineToolStripMenuItem.Checked = true;
                    break;
                case Models.VoiceType.MASCULINE_MODERN:
                    modernMāoriToolStripMenuItem.Checked = true;
                    masculineToolStripMenuItem.Checked = true;
                    break;
                case null:
                    break;
            }
            
        }

        /// <summary>
        /// When the native tool strip item is clicked, changes the current user's language settings to use a native voice.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void nativeMāoriToolStripMenuItem_Click(object sender, EventArgs e)
        {
            nativeMāoriToolStripMenuItem.Checked = true;
            modernMāoriToolStripMenuItem.Checked = false;
            UserManagement.CurrentUser.changeVoiceToNative();                                     // Change the current user variable...
            UserManagement.getUser(UserManagement.CurrentUser.getName()).changeVoiceToNative();   // and the current user in the list of users.
            MessageBox.Show(changetext);
        }

        /// <summary>
        /// When the modern tool strip item is clicked, changes the current user's language settings to use a modern voice.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void modernMāoriToolStripMenuItem_Click(object sender, EventArgs e)
        {
            nativeMāoriToolStripMenuItem.Checked = false;
            modernMāoriToolStripMenuItem.Checked = true;
            UserManagement.CurrentUser.changeVoiceToModern();                                     // Change the current user variable...
            UserManagement.getUser(UserManagement.CurrentUser.getName()).changeVoiceToModern();   // and the current user in the list of users.
            MessageBox.Show(changetext);
        }

        /// <summary>
        /// When the feminine tool strip item is clicked, changes the current user's language settings to use a feminine voice.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void feminineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            feminineToolStripMenuItem.Checked = true;
            masculineToolStripMenuItem.Checked = false;
            UserManagement.CurrentUser.changeVoiceToFeminine();                                     // Change the current user variable...
            UserManagement.getUser(UserManagement.CurrentUser.getName()).changeVoiceToFeminine();   // and the current user in the list of users.
            MessageBox.Show(changetext);
        }

        /// <summary>
        /// When the masculine tool strip item is clicked, changes the current user's language settings to use a masculine voice.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void masculineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            feminineToolStripMenuItem.Checked = false;
            masculineToolStripMenuItem.Checked = true;
            UserManagement.CurrentUser.changeVoiceToMasculine();                                     // Change the current user variable...
            UserManagement.getUser(UserManagement.CurrentUser.getName()).changeVoiceToMasculine();   // and the current user in the list of users.
            MessageBox.Show(changetext);
        }

        /// <summary>
        /// When the user selects sign out, returns them to the login screen.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void signOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // There will always be only one login screen, and it will always be open if the program is running.
            Application.OpenForms.OfType<LoginScreen>().SingleOrDefault().Show();   
            ((MainFormInterface)Parent).closeThis();
        }

        /// <summary>
        /// When the user selects change password, launches the relevant window.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void changePasswordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangePasswordWindow changePswdForm = new ChangePasswordWindow();
            changePswdForm.ShowDialog(this);
        }

        /// <summary>
        /// When the user selects administrator console, launches the relevant window.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void consoleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AdminConsole().ShowDialog(this);
        }

        /// <summary>
        /// When the user selects upload recording, launches the relevant window.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void uploadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new RecordingUploadScreen().ShowDialog(this);
        }

        /// <summary>
        /// When the user selects file path configuration, launches the relevant window.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void foldersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new SystemConfig().ShowDialog(this);
        }

        /// <summary>
        /// When the user selects submit feedback, launches the relevant window.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void feedbackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new FeedbackScreen().ShowDialog(this);
        }

        /// <summary>
        /// When the user selects instruction manual, launches the browser for them to read the github wiki.
        /// This isn't the best solution, especially with the current state of th ewiki, but for now, it was the one being used in the stable version.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void instructionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Open the browser to view the github wiki, at least until we get a proper system for this.
            IoController.ShowInBrowser("https://github.com/JSCooke/MPAi/wiki");
        }

        /// <summary>
        /// When the user selects about this program, displays details of the program in a message box.
        /// </summary>
        /// <param name="sender">Automatically generated by Visual Studio.</param>
        /// <param name="e">Automatically generated by Visual Studio.</param>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                this, "Maori Pronunciation Aid (MPAi) " +
                Application.ProductVersion + "\n\n" +
                "Dr. Catherine Watson\n" +
                "The University of Auckland",
                "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
