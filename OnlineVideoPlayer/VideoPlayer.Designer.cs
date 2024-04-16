namespace OnlineVideoPlayer
{
    partial class VideoPlayer
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VideoPlayer));
            this.MetaDataTimer = new System.Windows.Forms.Timer(this.components);
            this.axMediaPlayer = new AxWMPLib.AxWindowsMediaPlayer();
            this.GifPictureBox = new System.Windows.Forms.PictureBox();
            this.VideoPanel = new System.Windows.Forms.Panel();
            this.WaitForPlayTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.axMediaPlayer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GifPictureBox)).BeginInit();
            this.VideoPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // MetaDataTimer
            // 
            this.MetaDataTimer.Enabled = true;
            this.MetaDataTimer.Interval = 1000;
            this.MetaDataTimer.Tick += new System.EventHandler(this.MetaDataTimer_Tick);
            // 
            // axMediaPlayer
            // 
            resources.ApplyResources(this.axMediaPlayer, "axMediaPlayer");
            this.axMediaPlayer.Name = "axMediaPlayer";;
            this.axMediaPlayer.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axMediaPlayer.OcxState")));
            this.axMediaPlayer.ClickEvent += new AxWMPLib._WMPOCXEvents_ClickEventHandler(this.axWindowsMediaPlayer1_ClickEvent);
            this.axMediaPlayer.DoubleClickEvent += new AxWMPLib._WMPOCXEvents_DoubleClickEventHandler(this.axWindowsMediaPlayer1_DoubleClickEvent);
            this.axMediaPlayer.KeyDownEvent += new AxWMPLib._WMPOCXEvents_KeyDownEventHandler(this.axWindowsMediaPlayer1_KeyDownEvent);
            // 
            // GifPictureBox
            // 
            this.GifPictureBox.BackColor = System.Drawing.Color.Silver;
            resources.ApplyResources(this.GifPictureBox, "GifPictureBox");
            this.GifPictureBox.Name = "GifPictureBox";
            this.GifPictureBox.TabStop = false;
            // 
            // VideoPanel
            // 
            this.VideoPanel.AccessibleRole = System.Windows.Forms.AccessibleRole.OutlineButton;
            this.VideoPanel.BackColor = System.Drawing.SystemColors.GrayText;
            this.VideoPanel.Controls.Add(this.GifPictureBox);
            this.VideoPanel.Controls.Add(this.axMediaPlayer);
            resources.ApplyResources(this.VideoPanel, "VideoPanel");
            this.VideoPanel.Name = "VideoPanel";
            // 
            // WaitForPlayTimer
            // 
            this.WaitForPlayTimer.Interval = 1000;
            this.WaitForPlayTimer.Tick += new System.EventHandler(this.WaitForPlayTimer_Tick);
            // 
            // VideoPlayer
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.VideoPanel);
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Name = "VideoPlayer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VideoPlayer_FormClosing);
            this.Load += new System.EventHandler(this.VideoPlayer_Load);
            this.Shown += new System.EventHandler(this.VideoPlayer_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.axMediaPlayer)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GifPictureBox)).EndInit();
            this.VideoPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer MetaDataTimer;
        private AxWMPLib.AxWindowsMediaPlayer axMediaPlayer;
        private System.Windows.Forms.PictureBox GifPictureBox;
        private System.Windows.Forms.Panel VideoPanel;
        private System.Windows.Forms.Timer WaitForPlayTimer;
    }
}

