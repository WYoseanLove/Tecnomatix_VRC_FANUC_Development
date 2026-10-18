namespace VRC_FANUC
{
    partial class TxVRCRoboGuideComForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.robotName = new System.Windows.Forms.Label();
            this.socketAdress = new System.Windows.Forms.Label();
            this.m_Socketadd = new System.Windows.Forms.TextBox();
            this.m_robotPicked = new Tecnomatix.Engineering.Ui.TxObjEditBoxCtrl();
            this.m_Information = new System.Windows.Forms.TextBox();
            this.m_connect = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // robotName
            // 
            this.robotName.AutoSize = true;
            this.robotName.Location = new System.Drawing.Point(13, 60);
            this.robotName.Name = "robotName";
            this.robotName.Size = new System.Drawing.Size(166, 24);
            this.robotName.TabIndex = 0;
            this.robotName.Text = "Target Robot:";
            // 
            // socketAdress
            // 
            this.socketAdress.AutoSize = true;
            this.socketAdress.Location = new System.Drawing.Point(17, 123);
            this.socketAdress.Name = "socketAdress";
            this.socketAdress.Size = new System.Drawing.Size(214, 24);
            this.socketAdress.TabIndex = 1;
            this.socketAdress.Text = "Socket Listening:";
            // 
            // m_Socketadd
            // 
            this.m_Socketadd.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.m_Socketadd.Location = new System.Drawing.Point(265, 123);
            this.m_Socketadd.Name = "m_Socketadd";
            this.m_Socketadd.ReadOnly = true;
            this.m_Socketadd.Size = new System.Drawing.Size(431, 35);
            this.m_Socketadd.TabIndex = 3;
            // 
            // m_robotPicked
            // 
            this.m_robotPicked.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.m_robotPicked.KeepFaceEmphasizedWhenControlIsNotFocused = true;
            this.m_robotPicked.ListenToPick = true;
            this.m_robotPicked.Location = new System.Drawing.Point(265, 60);
            this.m_robotPicked.Name = "m_robotPicked";
            this.m_robotPicked.Object = null;
            this.m_robotPicked.PickAndClear = false;
            this.m_robotPicked.PickLevel = Tecnomatix.Engineering.Ui.TxPickLevel.Component;
            this.m_robotPicked.PickOnly = false;
            this.m_robotPicked.ReadOnly = false;
            this.m_robotPicked.Size = new System.Drawing.Size(431, 34);
            this.m_robotPicked.TabIndex = 4;
            this.m_robotPicked.ValidatorType = Tecnomatix.Engineering.Ui.TxValidatorType.Robot;
            this.m_robotPicked.Picked += new Tecnomatix.Engineering.Ui.TxObjEditBoxCtrl_PickedEventHandler(this.m_robotPicked_Picked);
            // 
            // m_Information
            // 
            this.m_Information.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.m_Information.Location = new System.Drawing.Point(21, 210);
            this.m_Information.Multiline = true;
            this.m_Information.Name = "m_Information";
            this.m_Information.ReadOnly = true;
            this.m_Information.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.m_Information.Size = new System.Drawing.Size(675, 367);
            this.m_Information.TabIndex = 5;
            // 
            // m_connect
            // 
            this.m_connect.Enabled = false;
            this.m_connect.Location = new System.Drawing.Point(554, 630);
            this.m_connect.Name = "m_connect";
            this.m_connect.Size = new System.Drawing.Size(142, 42);
            this.m_connect.TabIndex = 6;
            this.m_connect.Text = "Connect";
            this.m_connect.UseVisualStyleBackColor = true;
            this.m_connect.Click += new System.EventHandler(this.m_connect_Click);
            // 
            // TxVRCRoboGuideComForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(752, 718);
            this.Controls.Add(this.m_connect);
            this.Controls.Add(this.m_Information);
            this.Controls.Add(this.m_robotPicked);
            this.Controls.Add(this.m_Socketadd);
            this.Controls.Add(this.socketAdress);
            this.Controls.Add(this.robotName);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TxVRCRoboGuideComForm";
            this.SemiModal = false;
            this.ShouldCloseOnDocumentUnloading = true;
            this.ShowIcon = false;
            this.Text = "TxVRCRoboGuideComForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label robotName;
        private System.Windows.Forms.Label socketAdress;
        private System.Windows.Forms.TextBox m_Socketadd;
        private Tecnomatix.Engineering.Ui.TxObjEditBoxCtrl m_robotPicked;
        private System.Windows.Forms.TextBox m_Information;
        private System.Windows.Forms.Button m_connect;
    }
}